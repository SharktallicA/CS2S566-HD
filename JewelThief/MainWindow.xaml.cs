using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Diagnostics;
using System.Windows.Threading;
using Microsoft.Win32;
using System.IO;
using System.Windows.Media.Imaging;

namespace WizardDungeon
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow: Window
    {
        ///////////////////////////////////////////////////////////
        // These variables are all used by the game engine to
        // run the dynamics of the game.

        DispatcherTimer countDown = null;
        DispatcherTimer timer = null;
        Stopwatch watch = new Stopwatch();
        long previous_time = 0;
        int time_ellapsed = 0;
        bool IsPlaying = false;
        bool LevelLoaded = false;


        ///////////////////////////////////////////////////////////
        // The game engine can be used to update the game state. The 
        // game state represents the position and velocities of the
        // player and enemies. Note the enemies do not have a velocity
        // defined as part of the level.

        CGameEngine     gameEngine;
        CGameState      gameState;


        ///////////////////////////////////////////////////////////
        // The level represents the position of walls and floors
        // and contains starting positions of the player and enemy and goal.
        // The game textures stores all the images used to represent
        // different icons.

        CLevel          currentLevel;
        CGameTextures   gameTextures;


        ////////////////////////////////////////////////////////////
        // We keep a reference to these as we need to be able to 
        // update their position in the canvas. We do this by passing
        // a reference to the canvas (e.g. Canvas.SetLeft(enemyIcons[i], Position X);)

        Image[]         enemyIcons;
        Image[]         fireIcons;
        Image           playerIcon;


        /////////////////////////////////////////////////////////////
        // These represent the player and monster speed per frame 
        // (in units of tiles).

        float           player_speed  = 0.15f;
        float           monster_speed = 0.07f;

        public MainWindow()
        {
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            InitializeComponent();
        }

        //****************************************************************//
        // This function is used as an event handler for the load click  *//
        // event. This is used to load a new level. In addition to       *//
        // the data required for the level it also ensures the board is  *//
        // displayed.                                                    *//
        //****************************************************************//
        private void btnLoad_Click(object sender,RoutedEventArgs e)
        {
            ///////////////////////////////////////////////////////////
            // Get the directory where the level data is stored and 
            // load the data in. 

            string fileDir = txtLevelDir.Text;
            currentLevel = CLevelParser.ImportLevel(fileDir);
            gameTextures = CLevelParser.ImportTextures(fileDir);

            //enable certain controls now that level is loaded
            btnStart.IsEnabled = true;
            btnSave.IsEnabled = true;

            // Call unified/custom render function (centralised in Render() to cut down on code duplication)
            Render();

            //display level's time limit in text box
            txtTimeLimit.Text = currentLevel.Time.ToString();

            //flag level as loaded
            LevelLoaded = true;
        }


        //****************************************************************//
        // This initilaises the dynamic parts of the game to their initial//
        // positions as specified by the game level.                      //
        //****************************************************************//
        private void InitialiseGameState()
        {
            ////////////////////////////////////////////////////////////
            // Place the player at their initial position.

            gameState.Player.Position = CLevelUtils.GetPixelFromTileCoordinates(currentLevel.StartPosition);

            Random random = new Random();

            for(int i=0; i<currentLevel.EnemyPositions.Count(); i++)
            {
                ////////////////////////////////////////////////////////////
                // Place each enemy at their initial position and give them an
                // initial random direction.

                gameState.Enemies[i].Position = CLevelUtils.GetPixelFromTileCoordinates(currentLevel.EnemyPositions[i]);

                gameState.Enemies[i].TargetPosition.X = gameState.Enemies[i].Position.X;
                gameState.Enemies[i].TargetPosition.Y = gameState.Enemies[i].Position.Y;


                /////////////////////////////////////////////////////////////
                // Create a random direction to walk in.

                int direction = random.Next()%4;

                switch(direction)
                {
                    case 0:
                        gameState.Enemies[i].Velocity.X = monster_speed;
                        gameState.Enemies[i].Velocity.Y = 0.0f;
                        break;

                    case 1:
                        gameState.Enemies[i].Velocity.X =-monster_speed;
                        gameState.Enemies[i].Velocity.Y = 0.0f;
                        break;

                    case 2:
                        gameState.Enemies[i].Velocity.X = 0.0f;
                        gameState.Enemies[i].Velocity.Y = monster_speed;
                        break;

                    case 3:
                    default:
                        gameState.Enemies[i].Velocity.X = 0.0f;
                        gameState.Enemies[i].Velocity.Y =-monster_speed;
                        break;
                }
            }
        }


        //****************************************************************//
        // This function renders the dynamic content of the game to the  *//
        // main canvas. This is done by updating the positions of all    *//
        // the sprite icons in the canvas using the current game state   *//
        // and then invoking the canvas to refresh.                      *//
        //****************************************************************//
        private void RenderGameState()
        {
            Canvas.SetLeft(playerIcon, gameState.Player.Position.X);
            Canvas.SetTop (playerIcon, gameState.Player.Position.Y);


            for(int i=0; i<currentLevel.EnemyPositions.Count(); i++)
            {
                Canvas.SetLeft(enemyIcons[i], gameState.Enemies[i].Position.X);
                Canvas.SetTop (enemyIcons[i], gameState.Enemies[i].Position.Y);
            } 
        }


        //****************************************************************//
        // This function draws the static parts of the level onto the    *//
        // canvas.                                                       *//
        //****************************************************************//
        private void DrawLevel()
        {
            /////////////////////////////////////////////////////////////
            // Compute the width of the canvas, this will be the number
            // of tiles multiplied by the tile size (in pixels).

            int width   = currentLevel.Width*CGameTextures.TILE_SIZE;
            int height  = currentLevel.Height*CGameTextures.TILE_SIZE;

            cvsMainScreen.Width     = width;
            cvsMainScreen.Height    = height;


            /////////////////////////////////////////////////////////////
            // Loop through the level setting each tiled position on the 
            // canvas.

            for(int y=0; y<currentLevel.Height; y++)
            {
                for (int x=0; x<currentLevel.Width; x++)
                {
                    /////////////////////////////////////////////////////////
                    // We must create a new instance of the image as an image
                    // can only be added once to a given canvas.

                    Image texture   = new Image();
                    texture.Width   = CGameTextures.TILE_SIZE;
                    texture.Height  = CGameTextures.TILE_SIZE;


                    //////////////////////////////////////////////////////////
                    // Set the position of the tile, we must convert from tile
                    // coordinates to pixel coordinates.

                    CPoint2i tilePosition = CLevelUtils.GetPixelFromTileCoordinates(new CPoint2i(x,y));

                    
                    Canvas.SetLeft(texture, tilePosition.X);
                    Canvas.SetTop (texture, tilePosition.Y);
                    

                    //////////////////////////////////////////////////////////
                    // Check whether it should be a wall tile or floor tile.

                    if(currentLevel.GetTileType(x,y) == eTileType.Wall)
                    {
                       texture.Source = gameTextures.WallTexture;
                    }
                    else
                    {
                        texture.Source = gameTextures.FloorTexture;
                    }

                    cvsMainScreen.Children.Add(texture);
                }
            }


            ////////////////////////////////////////////////////////////
            // The goal is also static as it does not move so we will
            // also add this now also.

            Image goalImg   = new Image();
            goalImg.Width   = CGameTextures.TILE_SIZE;
            goalImg.Height  = CGameTextures.TILE_SIZE;

            goalImg.Source  = gameTextures.GoalIcon;

            CPoint2i GoalPosition = CLevelUtils.GetPixelFromTileCoordinates(new CPoint2i(currentLevel.GoalPosition.X, currentLevel.GoalPosition.Y));

            Canvas.SetLeft(goalImg, GoalPosition.X);
            Canvas.SetTop (goalImg, GoalPosition.Y);

            cvsMainScreen.Children.Add(goalImg);
        }


        //****************************************************************//
        // This event handler is used to handle a key being pressed.     *//
        // It only takes action if a direction key is pressed.           *//
        //****************************************************************//
        private void tb_KeyDown(object sender, KeyEventArgs args)
        {
            //////////////////////////////////////////////////////////
            // We set the players velocity, this controls which direction
            // it will move in.

            if(Keyboard.IsKeyDown(Key.Up))
            {
                gameState.Player.Velocity.Y=-player_speed;
                gameState.Player.Velocity.X= 0.0f;
            }
            else if(Keyboard.IsKeyDown(Key.Down))
            {
                gameState.Player.Velocity.Y= player_speed;
                gameState.Player.Velocity.X= 0.0f;
            }
            else if(Keyboard.IsKeyDown(Key.Left))
            {
                gameState.Player.Velocity.Y= 0.0f;
                gameState.Player.Velocity.X=-player_speed;
            }
            else if(Keyboard.IsKeyDown(Key.Right))
            {
                gameState.Player.Velocity.Y= 0.0f;
                gameState.Player.Velocity.X= player_speed;
            }
        }

        //****************************************************************//
        // This event handler is used to handle a key being released.    *//
        // We presume the direction key is being released.               *//
        //****************************************************************//
        private void tb_KeyUp(object sender, KeyEventArgs args)
        {
            ///////////////////////////////////////////////////////////////
            // By setting a player's velocity to zero it will not move.
            if(args.Key == Key.Up || args.Key == Key.Down || args.Key == Key.Left || args.Key == Key.Right)
            {
                gameState.Player.Velocity.X = 0.0f;
                gameState.Player.Velocity.Y = 0.0f;
            }
            
        }


        //****************************************************************//
        // This event handler is used to start the game.                 *//
        //****************************************************************//
        private void btnStart_Click(object sender, RoutedEventArgs e)
        {
            if (currentLevel == null)
            {
                MessageBox.Show("You must load a level before starting a game!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            lblMsg.Text = "";

            watch.Reset();
            watch.Start();
            previous_time = 0;


            //////////////////////////////////////////////////////////////
            // Create a new game engine to handle the interactions and 
            // register events to handle collisions with enemies or
            // winning the game.


            gameEngine = new CGameEngine(currentLevel);
            gameEngine.OnGoalReached  += EndGame;
            gameEngine.OnPlayerCaught += EndGame;


            //////////////////////////////////////////////////////////////
            // The game is rendered by using a dispatchTimer. This will 
            // trigger the game loop (RunTime) every 40ms.

            if(timer == null)
            {
                timer = new DispatcherTimer();
                timer.Tick += RunGame;
                timer.Interval = new TimeSpan(0,0,0,0,40);

                countDown = new DispatcherTimer();
                countDown.Tick += UpdateTime;
                countDown.Interval = new TimeSpan(0,0,0,1,0);
            }
            time_ellapsed=0;
            countDown.Start();
            timer.Start();



            /////////////////////////////////////////////////////////////
            // Make some of the elements on screen disabled so that the
            // user can't restart mid game play.

            IsPlaying = true;
            txtLevelDir.IsEnabled   = !IsPlaying;
            btnStart.IsEnabled      = !IsPlaying;
            btnLoad.IsEnabled       = !IsPlaying;
            btnSave.IsEnabled = !IsPlaying;
            btnEnd.IsEnabled        = IsPlaying;
            btnFind.IsEnabled = !IsPlaying;

            Keyboard.Focus(cvsMainScreen);

            ////////////////////////////////////////////////////////////
            // Set each instance of a dynamic object to its initial position.

            InitialiseGameState();


            ////////////////////////////////////////////////////////////
            // Render the current game state, this will render the player
            // and the enemies in their initial position.

            RenderGameState();
        }

        private void UpdateTime(object sender, object o)
        {
            time_ellapsed++;

            if(time_ellapsed < currentLevel.Time)
                lblMsg.Text = "Time Remaining: " + (currentLevel.Time-time_ellapsed);
            else
            {
                EndGame(this, "You lose, you ran out of time!");
            }
        }

        //****************************************************************//
        // This function represents the main game loop. It computes the   //
        // time between this and the previous call and then updates the   //
        // game state. It then requests the new game state to be          //
        // rendered.                                                      //
        //****************************************************************//
        private void RunGame(object sender, object o)
        {
            //////////////////////////////////////////////////////////////
            // Compute the difference in time between two consecutive 
            // calls.

            long current_time = watch.ElapsedMilliseconds;
            long time_delta = current_time - previous_time;
            previous_time = current_time;


            //////////////////////////////////////////////////////////////
            // Update and render the game.

            gameEngine.UpdateVelocities(gameState, (float)time_delta);
            gameEngine.UpdatePositions(gameState,  (float)time_delta);

            RenderGameState();
        }


        //****************************************************************//
        // This function will be registered to be triggered when the game //
        // finishes. It re-enables any buttons and displays a message to  //
        // indicate the end result of the game.                           //
        //****************************************************************//
        public void EndGame(object sender, string message)
        {
            countDown.Stop();
            lblMsg.Text = message;
            timer.Stop();
            IsPlaying = false;
            txtLevelDir.IsEnabled   = !IsPlaying;
            btnStart.IsEnabled      = !IsPlaying;
            btnLoad.IsEnabled       = !IsPlaying;
            btnSave.IsEnabled       = !IsPlaying;
            btnEnd.IsEnabled = IsPlaying;
            btnFind.IsEnabled = !IsPlaying;
        }

        /// <summary>
        /// itmExit callback: allows user to close program through menu
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ExitItem_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        /// <summary>
        /// btnFind callback: allows user to find a folder that contains a level
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnFind_Click(object sender, RoutedEventArgs e)
        {
            //initialise FileIO as OpenFileDialog
            FileIO dlgOpen = new FileIO(DialogType.Open, "Find level", "Level File|Level.txt");

            if (dlgOpen.ShowDialog())
            {
                //if dialog successful, stri
                txtLevelDir.Text = dlgOpen.FileName.Substring(0, dlgOpen.FileName.Length - "Level.txt".Length);
                btnLoad_Click(null, new RoutedEventArgs());
            }
        }

        /// <summary>
        /// Draws level unto canvas and ensures all labels and item image previews are accurate
        /// </summary>
        private void Render()
        {
            ////////////////////////////////////////////////////////////
            // clear any existing children from the canvas.

            cvsMainScreen.Children.Clear();

            ///////////////////////////////////////////////////////////
            // Draw the set of wall and floor tiles for the current
            // level and the goal icon. This is part of the game
            // we do not expect to change as it cannot move.

            DrawLevel();

            //////////////////////////////////////////////////////////
            // Add a game state, this represents the position and velocity
            // of all the enemies and the player. Basically, anything
            // that is dynamic that we expect to move around.

            gameState = new CGameState(currentLevel.EnemyPositions.Count());

            ///////////////////////////////////////////////////////////
            // Set up the player to have the correct .bmp and set it to 
            // its initial starting point. The player's position is stored
            // as a tile index on the Clevel class, this must be converted 
            // to a pixel position on the game state.

            playerIcon = new Image();
            playerIcon.Width = CGameTextures.TILE_SIZE;
            playerIcon.Height = CGameTextures.TILE_SIZE;

            playerIcon.Source = gameTextures.PlayerIcon;

            cvsMainScreen.Children.Add(playerIcon);


            //////////////////////////////////////////////////////////
            // Create instances of the enemies and fires for display. We must do
            // this as each child on a canvas must be a distinct object,
            // we could not simply add the same image multiple times.

            enemyIcons = new Image[currentLevel.EnemyPositions.Count()];

            for (int i = 0; i < currentLevel.EnemyPositions.Count(); i++)
            {
                enemyIcons[i] = new Image();

                enemyIcons[i].Width = CGameTextures.TILE_SIZE;
                enemyIcons[i].Height = CGameTextures.TILE_SIZE;

                enemyIcons[i].Source = gameTextures.EnemyIcon;

                cvsMainScreen.Children.Add(enemyIcons[i]);
            }


            fireIcons = new Image[currentLevel.FirePositions.Count()];

            for (int i = 0; i < currentLevel.FirePositions.Count(); i++)
            {
                fireIcons[i] = new Image();

                fireIcons[i].Width = CGameTextures.TILE_SIZE;
                fireIcons[i].Height = CGameTextures.TILE_SIZE;

                fireIcons[i].Source = gameTextures.FireIcon;

                cvsMainScreen.Children.Add(fireIcons[i]);

                CPoint2i tilePosition = CLevelUtils.GetPixelFromTileCoordinates(new CPoint2i(currentLevel.FirePositions[i].X, currentLevel.FirePositions[i].Y));


                Canvas.SetLeft(fireIcons[i], tilePosition.X);
                Canvas.SetTop(fireIcons[i], tilePosition.Y);
            }


            ////////////////////////////////////////////////////////////
            // Set each instance of a dynamic object to its initial position
            // as defined by the current level object.

            InitialiseGameState();


            ////////////////////////////////////////////////////////////
            // Render the current game state, this will render the player
            // and the enemies in their initial position.

            RenderGameState();

            ////////////////////////////////////////////////////////////
            // Indicate that a level has been loaded

            lblMsg.Text = "Level loaded!";


            ////////////////////////////////////////////////////////////
            // Update level counters on form

             DisplayLevelStats();

            ////////////////////////////////////////////////////////////
            // Add loaded textures and icon to Designer image previews

            imgFloorTile.Source = gameTextures.FloorTexture;
            imgWallTile.Source = gameTextures.WallTexture;
            imgFire.Source = gameTextures.FireIcon;
            imgEnemy.Source = gameTextures.EnemyIcon;
            imgPlayer.Source = gameTextures.PlayerIcon;
            imgGoal.Source = gameTextures.GoalIcon;
        }

        /// <summary>
        /// cvsMainScreen callback: handles executing a click-place/click-toggle task when user clicks on canvas
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void cvsMainScreen_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (LevelLoaded && !IsPlaying)
            {
                //get point of click for translating into a tile coord
                Point p = e.GetPosition(cvsMainScreen);
                CPoint2i pixelPos = new CPoint2i((int)p.X, (int)p.Y);
                CPoint2i mapPos = CLevelUtils.GetTileCoordinatesFromPixel(pixelPos);

                if (radTilePlacing.IsChecked == true)
                {
                    //toggle solid structure type (wall/floor)
                    if (currentLevel.GetTileType(mapPos.X, mapPos.Y) == eTileType.Wall) currentLevel.SetTileType(mapPos.X, mapPos.Y, eTileType.Floor);
                    else currentLevel.SetTileType(mapPos.X, mapPos.Y, eTileType.Wall);
                }
                else if (radFirePlacing.IsChecked == true)
                {
                    if (!currentLevel.FirePositions.Exists(itm => itm.X == mapPos.X && itm.Y == mapPos.Y)) currentLevel.FirePositions.Add(mapPos);
                    else currentLevel.FirePositions.RemoveAt(currentLevel.FirePositions.FindIndex(itm => itm.X == mapPos.X && itm.Y == mapPos.Y));
                }

                //call for game to be re-rendered to reflect changes
                Render();
            }
            else MessageBox.Show("You must end the game before altering the level!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        /// <summary>
        /// Updates status bar labels with appropriate level statistics
        /// </summary>
        private void DisplayLevelStats()
        {
            ////////////////////////////////////////////////////////////
            // call and utilise tile counter for floor and wall

            lblFloors.Text = "Floors: " + currentLevel.GetTileCount(eTileType.Floor);
            lblWalls.Text = "Walls: " + currentLevel.GetTileCount(eTileType.Wall);

            ////////////////////////////////////////////////////////////
            //get count from enemy and fire list container

            lblEnemies.Text = "Enemies: " + currentLevel.EnemyPositions.Count;
            lblFires.Text = "Fires: " + currentLevel.FirePositions.Count;

            ////////////////////////////////////////////////////////////
            //make label separators on the StatusBar visible

            sepOne.Visibility = Visibility.Visible;
            sepTwo.Visibility = Visibility.Visible;
        }

        /// <summary>
        /// End button clicked callback: ends the current game being played
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnEnd_Click(object sender, RoutedEventArgs e)
        {
            if (LevelLoaded &&IsPlaying)
            {
                Render();
                EndGame(null, "Game ended at user request");
            }
        }

        /// <summary>
        /// Set button clicked callback: handles when any "Set" button is clicked under "Item Images"
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnSetImage_Click(object sender, RoutedEventArgs e)
        {
            if (LevelLoaded && !IsPlaying)
            {
                //cast local reference of sender
                Button btnSender = sender as Button;

                //check sender's name to indicate what we're setting
                if (btnSender.Name.Contains("Floor"))
                    if (GetImage(imgFloorTile, "floor")) gameTextures.FloorTexture = imgFloorTile.Source as BitmapImage;
                else if (btnSender.Name.Contains("Wall"))
                    if (GetImage(imgWallTile, "wall")) gameTextures.WallTexture = imgWallTile.Source as BitmapImage;
                else if (btnSender.Name.Contains("Player"))
                    if (GetImage(imgPlayer, "player")) gameTextures.PlayerIcon = imgPlayer.Source as BitmapImage;
                else if (btnSender.Name.Contains("Goal"))
                    if (GetImage(imgGoal, "goal")) gameTextures.GoalIcon = imgGoal.Source as BitmapImage;
                else if (btnSender.Name.Contains("Fire"))
                    if (GetImage(imgFire, "fire")) gameTextures.FireIcon = imgFire.Source as BitmapImage;
                else if (btnSender.Name.Contains("Enemy"))
                    if (GetImage(imgEnemy, "enemy")) gameTextures.EnemyIcon = imgEnemy.Source as BitmapImage;

                //render results
                Render();
            }
        }

        /// <summary>
        /// Attempts to get image for a tile or item
        /// </summary>
        /// <param name="receiver">Image object to set the source of</param>
        /// <param name="name">Name of item that needs the image</param>
        /// <returns>Returning true indicates getting image was successful and specific texture needs updating</returns>
        private bool GetImage(Image receiver, string name)
        {
            //open FileIO instance as an OpenFileDialog
            FileIO dlgOpen = new FileIO(DialogType.Open, "Find " + name + " image", "Bitmap|*.bmp|Portable Network Graphics|*.png|Other|*.*");

            if (dlgOpen.ShowDialog())
            {
                //if dialog successful, set receiver's source as the result's filename as BitmapImage
                receiver.Source = new BitmapImage(new Uri(dlgOpen.FileName));
                return true;
            }
            else return false;
        }

        /// <summary>
        /// itmDesigner clicked callback: Allows Level Designer to be toggled in and out of view
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DesignerItem_Click(object sender, RoutedEventArgs e)
        {
            ////////////////////////////////////////////////////////////
            // Check the current checked status of itmDesigner to flip
            // its checked status, alter visibility of the Level Designer
            // panel, and ensure controls that can still be used to modify
            // the level outside the Level Designer panel are under control

            if (itmDesigner.IsChecked)
            {
                itmDesigner.IsChecked = false;
                itmNew.IsEnabled = false; //disallow accidental level creation when user cannot modify anything
                pnlDesigner.Visibility = Visibility.Hidden;
                radOff.IsChecked = true; //disallow accidental modification
            }
            else
            {
                itmDesigner.IsChecked = true;
                itmNew.IsEnabled = true;
                pnlDesigner.Visibility = Visibility.Visible;
            }
        }
    }
}
using System;
using System.Collections.Generic;

namespace WizardDungeon
{
    /// <summary>
    /// This enum characterises the type a tile can be
    /// in the game (e.g. wall or floor).
    /// </summary>
    enum eTileType
    {
        Wall,
        Floor
    };


    /// <summary>
    /// This enum defines whether a level should be flipped
    /// vertically or horizontally.
    /// </summary>
    public enum eFlipDirection
    {
        Horizontal,
        Vertical
    };


    /// <summary>
    /// This class is used to store all of the information about a level. This
    /// includes the level layout stored in m_levelMap and start positions of 
    /// the player and the enemies and the end goal.
    /// </summary>
    class CLevel
    {

        public int Height;
        public int Width;
        public int Time;
        public CPoint2i StartPosition { get; set; } = new CPoint2i();
        public CPoint2i GoalPosition  { get; set; } = new CPoint2i();
        public List<CPoint2i>  EnemyPositions { get; set; } = new List<CPoint2i>();
        public List<CPoint2i>  FirePositions { get; set; } = new List<CPoint2i>();

        private eTileType[,] m_levelMap = null;


        /// <summary>
        /// This constructor initialises an empty level object.
        /// </summary>
        public CLevel()
        {
            Height   = 0;
            Width   = 0;
            Time    = 0;
            EnemyPositions = new List<CPoint2i>();
            FirePositions = new List<CPoint2i>();
        }


        /// <summary>
        /// This function returns the tile type at the given tile
        /// location.
        /// </summary>
        /// <param name="x">The x tile coordinate.</param>
        /// <param name="y">The y tile corordinate</param>
        /// <returns>The tile type for the given coordinate.</returns>
        public eTileType GetTileType(int x, int y)
        {
          return m_levelMap[x, y];
        }


        /// <summary>
        /// This function sets the tile type at the given 
        /// coordinate to the given tile type.
        /// </summary>
        /// <param name="x">The x tile coordinate.</param>
        /// <param name="y">The y tile coordinate.</param>
        /// <param name="tile">The tile type.</param>
        public void SetTileType(int x, int y, eTileType tile)
        {
            m_levelMap[x, y] = tile;
        }


        /// <summary>
        /// This function flips the board in the given direction.
        /// </summary>
        /// <param name="direction"></param>
        public void Flip(eFlipDirection direction)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// This function resizes the board to the given size. 
        /// Currently this can only be used to initialise an
        /// empty board.
        /// </summary>
        /// <param name="sizeX">The width of the board.</param>
        /// <param name="sizeY">The length of the board.</param>
        public void Resize(int sizeX, int sizeY)
        {
            if(sizeX == Width && sizeY == Height)
            {
                return;
            }


            if(m_levelMap == null)
            {
                m_levelMap = new eTileType[sizeX, sizeY];

                for (int y=0; y<sizeY; y++)
                {
                    for (int x=0; x<sizeX; x++)
                    {
                        m_levelMap[x, y] = eTileType.Floor;
                    }
                }
            }
            else
            {
                ////////////////////////////////////////////////////////////
                // If a game is loaded, proceed by first creating a copy of 
                // the current map

                eTileType[,] previousMap = m_levelMap;

                ////////////////////////////////////////////////////////////
                // Initialise the new map

                m_levelMap = new eTileType[sizeX, sizeY];

                for (int y = 0; y < sizeY; y++)
                {
                    for (int x = 0; x < sizeX; x++)
                    {
                        ////////////////////////////////////////////////////////////
                        // Set new tile as a floor for a failsafe

                        m_levelMap[x, y] = eTileType.Floor;

                        ////////////////////////////////////////////////////////////
                        // See if previous map data can be reused

                        if (x < previousMap.GetLength(0) && y < previousMap.GetLength(1)) m_levelMap[x, y] = previousMap[x, y];
                    }
                }

                ////////////////////////////////////////////////////////////
                // Flag for use with entity out of bound issue checking:
                // Ie, suppose the map is reduced in size and some fires or enemies
                // are outside the new borders, they'd need to be removed

                bool complete = false;

                ////////////////////////////////////////////////////////////
                // Check if any enemies are now out of bounds due to the
                // map size change

                while (!complete)
                {
                    if (EnemyPositions.Count == 0) break; //if there are no enemies, break out of the checking process

                    for (int i = 0; i < EnemyPositions.Count; i++)
                    {
                        if (EnemyPositions[i].X >= sizeX || EnemyPositions[i].Y >= sizeY)
                        {
                            EnemyPositions.RemoveAt(i);
                            complete = false;
                            break;
                        }
                        else complete = true;
                    }
                }

                ////////////////////////////////////////////////////////////
                // Reset flag

                complete = false;

                ////////////////////////////////////////////////////////////
                // Check if any fires are now out of bounds due to the
                // map size change

                while (!complete)
                {
                    if (FirePositions.Count == 0) break; //if there are no fires, break out of the checking process

                    for (int i = 0; i < FirePositions.Count; i++)
                    {
                        if (FirePositions[i].X >= sizeX || FirePositions[i].Y >= sizeY)
                        {
                            FirePositions.RemoveAt(i);
                            complete = false;
                            break;
                        }
                        else complete = true;
                    }
                }

                ////////////////////////////////////////////////////////////
                // Check if start position is now out of bounds

                if (StartPosition != null) if (StartPosition.X >= sizeX || StartPosition.Y >= sizeY) StartPosition = null;

                ////////////////////////////////////////////////////////////
                // Check if end position is now out of bounds

                if (GoalPosition != null) if (GoalPosition.X >= sizeX || GoalPosition.Y >= sizeY) GoalPosition = null;
            }

            Width = sizeX;
            Height = sizeY;
        }

        /// <summary>
        /// Returns a count of the specified tile type
        /// </summary>
        /// <param name="type">Specified tile type</param>
        public int GetTileCount(eTileType type)
        {
            int Count = 0; //holds count of tile

            for (int i = 0; i < Width; i++)
            {
                for (int j = 0; j < Height; j++) if (m_levelMap[i, j] == type) Count++;
            }
            return Count;
        }
    }
}
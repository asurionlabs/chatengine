/*
AVA Chat Engine is a chat bot API.

Copyright (C) 2015-2019  Asurion, LLC

AVA Chat Engine is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

AVA Chat Engine is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with AVA Chat Engine.  If not, see <https://www.gnu.org/licenses/>.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatWeb.Services
{
    public class Levenshtein
    {
        // http://www.codeproject.com/Articles/13525/Fast-memory-efficient-Levenshtein-algorithm?fid=282908&df=90&mpp=25&prof=False&sort=Position&view=Normal&spc=Relaxed&fr=1

        public static int LevenshteinDistance(String value1, String value2)
        {
            return LevenshteinDistance(value1.ToCharArray(), value2.ToCharArray());
        }

        public static int LevenshteinDistance(char[] value1, char[] value2)
        {
            int rowLength = value1.Length;  
            int columnLength = value2.Length;
            int rowIndex;                // iterates through sRow
            int columnIndex;             // iterates through sCol
            char rowIthChar;             // ith character of sRow
            char columnJthChar;          // jth character of sCol
            int cost;                    // cost

            // Step 1
            if (rowLength == 0)
                return columnLength;

            if (columnLength == 0)
                return rowLength;

            // Create the two vectors
            int[] vector0 = new int[rowLength + 1];
            int[] vector1 = new int[rowLength + 1];
            int[] vectorTemp;

            // Step 2
            // Initialize the first vector
            for (rowIndex = 1; rowIndex <= rowLength; rowIndex++)
            {
                vector0[rowIndex] = rowIndex;
            }

            // Step 3

            // Fore each column
            for (columnIndex = 1; columnIndex <= columnLength; columnIndex++)
            {
                // Set the 0'th element to the column number
                vector1[0] = columnIndex;
                columnJthChar = value2[columnIndex - 1];


                // Step 4
                // For each row
                for (rowIndex = 1; rowIndex <= rowLength; rowIndex++)
                {
                    rowIthChar = value1[rowIndex - 1];

                    // Step 5
                    if (rowIthChar == columnJthChar)
                        cost = 0;
                    else
                        cost = 1;

                    // Step 6

                    // Find minimum
                    int minimum = vector0[rowIndex] + 1;
                    int b = vector1[rowIndex - 1] + 1;
                    int c = vector0[rowIndex - 1] + cost;

                    if (b < minimum)
                        minimum = b;

                    if (c < minimum)
                        minimum = c;

                    vector1[rowIndex] = minimum;
                }

                // Swap the vectors
                vectorTemp = vector0;
                vector0 = vector1;
                vector1 = vectorTemp;
            }

            return vector0[rowLength];
        }
    }
}

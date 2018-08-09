namespace ELO.Discord.Extensions
{
    using System.Collections.Generic;
    using System.Linq;

    public static class ListManagement
    {
        /// <summary>
        ///     Split a list into a group of lists of a specified size.
        /// </summary>
        /// <typeparam name="T">Type of item held within the list</typeparam>
        /// <param name="fullList">Input list</param>
        /// <param name="groupSize">Size of Groups to output</param>
        /// <returns>
        /// A list comprised of smaller sub-lists of the given type
        /// </returns>
        public static List<List<T>> SplitList<T>(this List<T> fullList, int groupSize = 30)
        {
            var splitList = new List<List<T>>();
            for (var i = 0; i < fullList.Count; i += groupSize)
            {
                splitList.Add(fullList.Skip(i).Take(groupSize).ToList());
            }

            return splitList;
        }
    }
}
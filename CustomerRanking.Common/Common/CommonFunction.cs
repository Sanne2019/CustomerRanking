
namespace CustomerRanking.Common.Common
{
    public class CommonFunction
    {
        /// <summary>
        /// get descending order key,because the default is ascending order
        /// </summary>
        /// <param name="score"></param>
        /// <param name="id"></param>
        /// <returns></returns>
        public static (decimal, long) GetKey(decimal score, long id)
        {
            return (-score, id);
        }
    }
}

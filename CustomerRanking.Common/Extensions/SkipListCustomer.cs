
namespace CustomerRanking.Common.Extensions
{
    /// <summary>
    /// SkipListCustomer: To facilitate ranking using skip lists.
    /// </summary>
    public class SkipListCustomer
    {
        public long CustomerId { get; set; }
        public decimal Score { get; set; }

        // Array length = number of levels;
        // NextArray[i] points to the next node of the current node at level i.
        public SkipListCustomer[] NextArray { get; set; }

        // Array length = number of levels;
        // NextSpanArray[i] stores the span (i.e., count of base-level nodes skipped) from current node to next node at level i.
        // Only at the first level (level 0), the span between adjacent nodes is always 1, because level 0 is a complete linked list containing all nodes.
        // At higher levels, some nodes may not appear, so the span between two adjacent nodes (x and y) in that level will be at least greater than 1.
        public int[] NextSpanArray { get; set; }

        public SkipListCustomer(int level, long customerId, decimal score)
        {
            CustomerId = customerId;
            Score = score;
            NextArray = new SkipListCustomer[level];
            NextSpanArray = new int[level];
        }
    }

    /// <summary>
    /// RankedSkipListCustomer: Efficient rank queries & order statistics via span tracking.
    /// </summary>
    public class RankedSkipListCustomer
    {
        // MaxLevel is the max number of levels in the skip list. 32 levels can hold about 2^32 nodes.
        // If MaxLevel is 16 and Ratio is 0.5, when data reaches 65,536 nodes, the average level is about 16.
        // (So, MaxLevel=16 and Ratio=0.5 is not the best.)
        // If MaxLevel is 32 and Ratio is 0.25, when data reaches 4.2 billion nodes, the average level is about 16.
        private const int MaxLevel = 32;
        private const double Ratio = 0.25;

        private readonly SkipListCustomer _header;
        private int _level;
        private int _count;
        private readonly Random _random;

        public RankedSkipListCustomer()
        {
            _header = new SkipListCustomer(MaxLevel, -1, 0); // Head node does not store real data,just a placeholder.
            _level = 1;
            _random = new Random();
        }

        /// <summary>
        ///  If scores are equal, then compare by id (smaller id comes first).
        /// </summary>
        private int Compare(decimal score1, long id1, decimal score2, long id2)
        {
            if (score1 != score2)
            {
                // for orderby score descending
                return score2.CompareTo(score1);
            }
            // for orderby id ascending
            return id1.CompareTo(id2);
        }

        /// <summary>
        /// With Ratio set to 0.25, there is a 25% chance that a new node will go up one level.
        /// A higher percentage means the new node is more likely to jump to higher levels.
        /// </summary>
        /// <returns></returns>
        private int RandomLevel()
        {
            int lvl = 1;
            while (_random.NextDouble() < Ratio && lvl < MaxLevel)
                lvl++;
            return lvl;
        }

        public void Insert(long customerId, decimal score)
        {
            // New node's predecessor node at each level. Later we will adjust steps between predecessor and new node.
            SkipListCustomer[] update = new SkipListCustomer[MaxLevel];

            // Records cumulative rank value passed at each level
            int[] rank = new int[MaxLevel];

            // Start with a dummy node
            SkipListCustomer x = _header;

            #region Step 1: Find the predecessor node where the new node will be inserted (i.e., find who we insert after)
            // 1. Search from the top level down to find the insertion point
            for (int i = _level - 1; i >= 0; i--)
            {
                // Carry over rank from the previous level to the current level
                rank[i] = (i == _level - 1) ? 0 : rank[i + 1];
                // Example: if there are 3 levels, rank[2] is 0.
                // If level 2 skips 10 steps, rank[1] = 10.
                // If level 1 skips 5 steps, rank[0] = 10 + 5 = 15.
                // rank can be seen as a cumulative rank value.

                while (x.NextArray[i] != null &&
                       Compare(x.NextArray[i].Score, x.NextArray[i].CustomerId, score, customerId) < 0)
                {
                    // This while loop: if only one level, compare existing data with new data.
                    // If new data is "less" than x.NextArray[i]'s value, keep looping.
                    // rank[i] accumulates the span of each node at this level.
                    // Starting from the top, the final rank[0] is the rank the new node will have in the entire skip list.
                    // When new data becomes "greater" than x, break the while loop.
                    // Assign x to update[i], meaning at level i, we will insert the new node after x.
                    rank[i] += x.NextSpanArray[i]; // Accumulate the span passed (for later span calculation)
                    x = x.NextArray[i];
                }
                update[i] = x;
            }
            // After each level operation, update[i] is the predecessor node at level i.
            // Since we insert after the predecessor, we later change the distance from the original predecessor to the new node,
            // and the distance from the new node to the next node.
            // Other distances stay the same; only update the predecessor and new node.
            #endregion

            #region Step 2: Decide by "coin toss" whether to promote the new node to higher levels.
            int newLevel = RandomLevel();
            if (newLevel > _level)
            {
                // If original level is 5 and new node is level 8,
                // set update[5], update[6], update[7] to header, meaning the predecessor at these levels is header.
                // The span for each update[i] is the current total count.
                // If new node's level is higher than current max level, initialize extra levels' update to header
                for (int i = _level; i < newLevel; i++)
                {
                    rank[i] = 0;
                    update[i] = _header;
                    // The span for the new level is temporarily set to current total count (meaning header to end span)
                    update[i].NextSpanArray[i] = _count;
                }
            }
            #endregion

            // Create the new node. Later we insert it at its position in each level.
            var newNode = new SkipListCustomer(newLevel, customerId, score);

            #region Step 3: The order in each level is clear, levels are built, and insertion point is found.
            // Now adjust the span relationship between the new node and its predecessor at each level.
            for (int i = 0; i < newLevel; i++)
            {
                newNode.NextArray[i] = update[i].NextArray[i]; // Tell the new node who its next node is.
                update[i].NextArray[i] = newNode; // The predecessor now points to the new node.

                // rank[0] is the rank the new node will have in the entire skip list.
                // rank[i] is the rank the new node will have at level i.
                // The rank at each level is different; lower levels have larger rank (i.e., further back).
                // rank[i] can also be seen as the predecessor's rank in the entire skip list.
                // rank[0] - rank[i] means: if total rank is 500 and at current level rank is 150, then there are 350 steps in between.
                // So the predecessor and new node are 350 steps apart.
                // Since new node x is inserted after update[i], use the predecessor's original span to next node minus these 350 steps
                // to get the distance from new node x to the next node.
                // The predecessor's next node is now new node x, so the span from predecessor to x equals (350) + 1 (include the new node itself).
                // Each element's span includes the next node but not itself, so x's span does not need +1 (does not include itself),
                // but update[i]'s span needs +1 (to include the new node).
                newNode.NextSpanArray[i] = update[i].NextSpanArray[i] - (rank[0] - rank[i]);
                update[i].NextSpanArray[i] = (rank[0] - rank[i]) + 1;
            }
            #endregion

            #region Step 4: Update spans at higher levels not touched by the new node
            // Only when old level is higher than new node's level, these levels need simple ++.
            // For example, if original has 5 levels and new node only goes to 3,
            // then levels 4 and 5 must each +1.
            for (int i = newLevel; i < _level; i++)
            {
                // For each level between existing level and total level,
                // increase the predecessor's span by 1 because the base level has one more node.
                // Even if the element does not appear at this level, the span from predecessor to next node must +1.
                update[i].NextSpanArray[i]++;
            }
            #endregion

            #region Step 5: Finally update global level and count
            if (newLevel > _level) _level = newLevel;
            _count++;
            #endregion
        }

        /// <summary>
        /// To delete an element, just modify the span and forward pointer of its predecessor.
        // The span is the steps to the next element.
        // If an element is deleted, the predecessor's span must add the deleted element's span and subtract 1 (because the deleted element itself takes one step).
        // The forward pointer originally pointed to the element to be deleted, now it should point to the deleted element's next element.
        /// </summary>
        /// <param name="customerId"></param>
        /// <param name="score"></param>
        /// <returns></returns>
        public bool Delete(long customerId, decimal score)
        {
            // update array records the predecessor of the target node at each level
            SkipListCustomer[] update = new SkipListCustomer[MaxLevel];
            SkipListCustomer x = _header;

            #region Step 1: Start from the top level, find the predecessor of the target element
            for (int i = _level - 1; i >= 0; i--)
            {
                while (x.NextArray[i] != null &&
                       Compare(x.NextArray[i].Score, x.NextArray[i].CustomerId, score, customerId) < 0)
                {
                    // Starting from the default predecessor at current level,
                    // as long as its next node is not null and the current user's score is less than the next node's score,
                    // the user is still to the right, keep moving right.
                    // When the current user's score becomes greater than the next node's score,
                    // the user should be to the left, break the while loop for this level and go to the next lower level.
                    x = x.NextArray[i];
                }
                update[i] = x; // update[i] records who is the predecessor at level i.
                               // After deleting the element, each level's predecessor needs two updates:
                               // 1. NextArray[i] should point to the deleted node's next node.
                               // 2. NextSpanArray[i] should become original span + deleted node's span - 1.
            }
            #endregion

            #region Step 2: Get the element to delete at level 0
            SkipListCustomer currentNode = x.NextArray[0];
            // After the above for loop, x is update[0],
            // so currentNode = update[0].NextArray[0] is the same.
            // But considering later updates to update[i].NextArray[i], it's easy to cause pointer confusion.
            #endregion

            #region Step 3: Check if the obtained node currentNode is indeed the one to delete by comparing score and id
            if (!(currentNode != null && currentNode.CustomerId == customerId && currentNode.Score == score))
            {
                return false;
            }
            #endregion

            #region Step 4: Update the NextArray and NextSpanArray of the predecessor at each level for the deleted element.
            // Update from bottom level up.
            for (int i = 0; i < _level; i++)
            {
                if (update[i].NextArray[i] == currentNode)
                {
                    // If the predecessor's next node at current level is the node to delete
                    // Then 1. The predecessor's span becomes: original span to the deleted node + the deleted node's span to its next node - 1.
                    // 1 is the step taken by the deleted node itself.
                    update[i].NextSpanArray[i] = update[i].NextSpanArray[i] + currentNode.NextSpanArray[i] - 1;
                    // The predecessor's next node becomes the deleted node's next node.
                    update[i].NextArray[i] = currentNode.NextArray[i];
                }
                else
                {
                    // If the predecessor's next node at current level is not the node to delete, then this level does not have the deleted node.
                    // For example, starting from low levels, when reaching level 5, if the predecessor's next node is not the node to delete,
                    // it means the deleted node does not appear at high levels.
                    // Then each high-level predecessor's span must decrease by 1,
                    // because update[i].NextSpanArray is the number of base-level nodes skipped from predecessor to next node.
                    // Since the base level lost one node, the predecessor at high levels must decrease by 1.
                    update[i].NextSpanArray[i] -= 1;
                }
            }
            #endregion

            #region Step 5: Start from the top level, if a level's predecessor's next node is null, that level has no nodes, so decrease the skip list level.
            while (_level > 1 && _header.NextArray[_level - 1] == null)
            {
                _level--;
            }
            #endregion

            #region Step 6: Every time an element is deleted, total count decreases by 1
            _count--;
            #endregion
            return true;
        }

        /// <summary>
        /// Get the rank of a user by customerId and score.
        /// Why need both id and score? Because the skip list is sorted by score first, then id.
        /// Without score, we cannot find the user's exact position in the skip list, so cannot calculate rank accurately.
        /// </summary>
        /// <param name="customerId"></param>
        /// <param name="score"></param>
        /// <returns></returns>
        public int GetRankById(long customerId, decimal score)
        {
            int currentRank = 0;
            SkipListCustomer x = _header;

            #region Start traversal from the top level
            for (int i = _level - 1; i >= 0; i--)
            {
                while (x.NextArray[i] != null)
                {
                    // If header's next node is not null, compare header's next node with target node.
                    int cmp = Compare(x.NextArray[i].Score, x.NextArray[i].CustomerId, score, customerId);
                    if (cmp < 0)
                    {
                        // If target node is "less" than header's next node, target is to the right.
                        // Add the span we have skipped, then move x to header's next node and continue right.
                        currentRank += x.NextSpanArray[i];
                        x = x.NextArray[i];
                    }
                    else if (cmp == 0)
                    {
                        // If target node equals header's next node, we found the target position.
                        currentRank += x.NextSpanArray[i];
                        return currentRank;
                    }
                    else
                    {
                        // If target node is "greater" than header's next node, target should be to the left.
                        // Break out of this level's loop and go to the next lower level.
                        break;
                    }
                }
            }
            #endregion
            return 0;
        }

        /// <summary>
        /// Get a node by rank. Once we get the node, we can get subsequent nodes because it's a linked structure.
        /// </summary>
        /// <param name="targetRank"></param>
        /// <returns></returns>
        public SkipListCustomer GetNodeByRank(int targetRank)
        {
            if (targetRank <= 0 || targetRank > _count) return null;

            SkipListCustomer x = _header;
            // Starting from top, record steps skipped. If steps skipped equals target rank, then current node is the target.
            int traversed = 0; 

            #region To get rank, start from top level, find large jumps first, then narrow down.
            for (int i = _level - 1; i >= 0; i--)
            {
                // At each level, move right as long as the right node is not null and (steps already traversed + span at current level) is less than or equal to target rank.
                while (x.NextArray[i] != null && (traversed + x.NextSpanArray[i]) <= targetRank)
                {
                    // From top level, accumulate the span from each element to its next node, left to right.
                    // After accumulating, move x to the next node, level by level.
                    traversed += x.NextSpanArray[i];
                    x = x.NextArray[i];
                }
                // If at some level the accumulated steps equal target rank, then current position is the target node, return it.
                if (traversed == targetRank) return x;
            }
            #endregion
            return x;
        }
    }
}
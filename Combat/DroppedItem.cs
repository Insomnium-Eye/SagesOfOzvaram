using Microsoft.Xna.Framework;

namespace SagesOfOzvaram.Combat
{
    /// <summary>
    /// An Item lying on the ground at a specific world position - e.g. the Meat a slain Wild
    /// beast drops (see Game1's death sweep) - waiting to be picked up via the "Pick Up" Action
    /// (TurnMenuBuilder/Game1.ExecutePickUp). Not owned by anyone until a unit actually picks it
    /// up; lives in Game1._droppedItems until then.
    /// </summary>
    public class DroppedItem
    {
        public Vector2 Position { get; set; }
        public Item Item { get; }

        public DroppedItem(Item item, Vector2 position)
        {
            Item = item;
            Position = position;
        }
    }
}

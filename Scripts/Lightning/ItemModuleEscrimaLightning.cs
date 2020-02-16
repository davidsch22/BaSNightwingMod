using BS;

namespace EscrimaLightning
{
    // This creates an item module that can be referenced in the item JSON
    public class ItemModuleEscrimaLightning : ItemModule
    {
        public float hitRadius = 3;
        public float damage = 10;
        public float duration = 1;
        public float cooldownTime = 3;
        public string lightningSFX = "None";

        public override void OnItemLoaded(Item item)
        {
            base.OnItemLoaded(item);
            item.gameObject.AddComponent<ItemEscrimaLightning>();
        }
    }
}

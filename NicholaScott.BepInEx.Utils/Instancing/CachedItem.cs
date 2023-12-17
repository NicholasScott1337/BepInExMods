namespace NicholaScott.BepInEx.Utils.Instancing
{
    public class CachedItem<TItem>
    {
        public delegate TItem InstanceLocator();
        public delegate bool ItemNotNull(TItem item);

        public TItem Value
        {
            get
            {
                if (valueAssigned && (notNullMethod == null || notNullMethod.Invoke(value))) return value;
                
                value = locatorMethod.Invoke();    
                valueAssigned = notNullMethod == null || notNullMethod.Invoke(value);

                return value;
            }
        } 
        
        private InstanceLocator locatorMethod;
        private ItemNotNull notNullMethod;
        private bool valueAssigned = false;
        private TItem value;
        
        public CachedItem(InstanceLocator locator, ItemNotNull notNull = null)
        {
            locatorMethod = locator;
            notNullMethod = notNull;
        }

        public void ClearCache() => valueAssigned = false;

        public static implicit operator TItem(CachedItem<TItem> item)
        {
            return item.Value;
        }
    }
}
namespace PentaWork.Tools.PluginGraph.Model.VMObjects
{
    public class ExecutionContextObj : IVMObj
    {
        public EntityObj GetItem(string name)
        {
            return new EntityObj
            {
                IsTarget = name == "Target"
            };
        }
    }
}

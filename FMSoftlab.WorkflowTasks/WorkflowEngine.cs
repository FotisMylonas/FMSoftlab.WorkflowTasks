namespace FMSoftlab.WorkflowTasks
{
    public interface IWorkflowEngine
    {
        Task Execute(BaseTask workflowTask);
    }

    public class WorkflowEngine : IWorkflowEngine
    {
        public async Task Execute(BaseTask workflowTask)
        {
            await workflowTask.DoExecute();
            foreach (var t in workflowTask.Tasks)
            {
                await Execute(t);
            }
        }
    }
   /* public interface IWorkflow
    {
        IDictionary<string, object> TaskResults { get; }
        List<BaseTask> Tasks { get; }
    }
    public class SequentialWokflow : IWorkflow
    {
        public IDictionary<string, object> TaskResults { get; }
        public List<BaseTask> Tasks { get; }
        public SequentialWokflow()
        {
            TaskResults=new Dictionary<string, object>();
            Tasks=new List<BaseTask>();
        }
    }*/

    






}
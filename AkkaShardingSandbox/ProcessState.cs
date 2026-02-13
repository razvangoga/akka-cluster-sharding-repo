namespace AkkaShardingSandbox;

public class ProcessState
{
    public ProcessState(string id)
    {
        this.Id = id;
    }

    public string Id { get; set; }
    public int Hits { get; set; }

}

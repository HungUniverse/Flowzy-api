namespace Flowzy.Service.Dashboard;
public interface IDashboardService{Task<object>Get(string view,string? email,string? term,string? course,long? groupId,string? status,int page,int size,int limit,CancellationToken ct);}

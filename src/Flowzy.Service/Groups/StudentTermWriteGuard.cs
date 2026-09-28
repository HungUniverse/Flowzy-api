using Flowzy.Repository.Entities;
using Flowzy.Service.Exceptions;

namespace Flowzy.Service.Groups;

public static class StudentTermWriteGuard
{
    public static void RequireWritable(StudentGroup group)
    {
        // Java tolerates unpopulated legacy/test relationships; persisted groups use the term FK.
        if (group.TermNavigation is not null && group.TermNavigation.Status != "OPEN")
            throw new ConflictException($"Academic term {group.Term} has ended; this group is read-only for students");
    }
}

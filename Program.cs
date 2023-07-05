using HotChocolate.Resolvers;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContextPool<ApplicationDbContext>(
    options => options.UseSqlServer(@"Server=(localdb)\mssqllocaldb;Database=Second_Operation_Testing;"));

builder.Services.AddScoped<IPersonService, PersonService>();
builder.Services.AddScoped<IAssignmentService, AssignmentService>();

builder.Services
    .AddGraphQLServer()
    .AddTypes()
    .AddQueryType<Query>()
    .RegisterService<IPersonService>(ServiceKind.Resolver)
    .RegisterService<IAssignmentService>(ServiceKind.Resolver);

var app = builder.Build();

app.MapGraphQL();
app.Run();

public interface IPersonService
{
    IQueryable<Person> GetPersons();
}

public class PersonService : IPersonService
{
    private readonly ApplicationDbContext _dbContext;

    public PersonService(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public IQueryable<Person> GetPersons() => _dbContext.Persons;
}

public class Person
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Age { get; set; }
    public string Address { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public List<Assignment> Assignments { get; set; }
}

public interface IAssignmentService
{
    IQueryable<Assignment> GetAssignments();
}

public class AssignmentService : IAssignmentService
{
    private readonly ApplicationDbContext _dbContext;

    public AssignmentService(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public IQueryable<Assignment> GetAssignments() => _dbContext.Assignments;
}

public class Assignment
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime DueDate { get; set; }
    public bool IsComplete { get; set; }
    public int PersonId { get; set; }
    public Person Person { get; set; } = null!;
}

[ExtendObjectType<Assignment>]
public class AssignmentExtensions
{
    public Task<Person> GetPersonCustomAsync(
        IPersonService personService,
        [Parent] Assignment assignment,
        IResolverContext context,
        CancellationToken cancellationToken)
    {
        return context.BatchDataLoader<int, Person>(
            async (keys, ct) =>
            {
                return await personService.GetPersons().Where(x => keys.Contains(x.Id)).ToDictionaryAsync(k => k.Id, ct);
            }
        ).LoadAsync(assignment.PersonId, cancellationToken);
    }
}

public class Query
{
    [UsePaging(MaxPageSize = 1000000)]
    public IQueryable<Assignment> GetAssignments(IAssignmentService assignmentService) => assignmentService.GetAssignments();
}

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options) { }

    public DbSet<Person> Persons { get; set; }
    public DbSet<Assignment> Assignments { get; set; }
}
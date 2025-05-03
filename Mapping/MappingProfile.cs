using AutoMapper;
using TaskTitan.Api.Dtos;
using TaskTitan.Data.Entities;
using TaskTitanData.Entities;
using Task = TaskTitanData.Entities.Task;

namespace TaskTitan.Api.Mappings
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            CreateMap<Customer, CustomerDto>();
            CreateMap<CreateCustomerDto, Customer>().ForMember(dest => dest.Id, opt => opt.Ignore());

            CreateMap<Project, ProjectDto>().ForMember(dest => dest.CustomerName, opt => opt.MapFrom(src => src.Customer != null ? src.Customer.Name : string.Empty));
            CreateMap<CreateProjectDto, Project>().ForMember(dest => dest.Id, opt => opt.Ignore()).ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => DateTime.UtcNow)).ForMember(dest => dest.Tasks, opt => opt.Ignore()).ForMember(dest => dest.Users, opt => opt.Ignore()).ForMember(dest => dest.Customer, opt => opt.Ignore());

            CreateMap<User, UserDto>();
            CreateMap<CreateUserDto, User>().ForMember(dest => dest.Id, opt => opt.Ignore()).ForMember(dest => dest.AssignedTasks, opt => opt.Ignore()).ForMember(dest => dest.Projects, opt => opt.Ignore());

            // Task <-> TaskDto map'lemesi IsDaily içeriyor
            CreateMap<Task, TaskDto>()
                .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status.ToString()))
                .ForMember(dest => dest.ProjectName, opt => opt.MapFrom(src => src.Project != null ? src.Project.Name : string.Empty))
                .ForMember(dest => dest.AssignedUserName, opt => opt.MapFrom(src => src.AssignedUser != null ? src.AssignedUser.Name : string.Empty))
                .ForMember(dest => dest.CreatedByUserName, opt => opt.MapFrom(src => src.CreatedByUser != null ? src.CreatedByUser.Name : string.Empty))
                .ForMember(dest => dest.IsDaily, opt => opt.MapFrom(src => src.IsDaily)); // Doğru

            // CreateTaskDto -> Task map'lemesi IsDaily içeriyor
            CreateMap<CreateTaskDto, Task>()
                .ForMember(dest => dest.Status, opt => opt.Ignore())
                .ForMember(dest => dest.IsDaily, opt => opt.MapFrom(src => src.IsDaily)) // Doğru
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.Project, opt => opt.Ignore())
                .ForMember(dest => dest.AssignedUser, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedByUser, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => DateTime.UtcNow))
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore());

            // UpdateTaskDto -> Task map'lemesi ForAllMembers ile IsDaily'yi de kapsar
            CreateMap<UpdateTaskDto, Task>()
                .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));
        }
    }
}
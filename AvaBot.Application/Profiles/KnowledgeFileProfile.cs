using AutoMapper;
using AvaBot.Domain.Models;
using AvaBot.DTO;

namespace AvaBot.Application.Profiles;

public class KnowledgeFileProfile : Profile
{
    public KnowledgeFileProfile()
    {
        CreateMap<KnowledgeFile, KnowledgeFileInfo>()
            .ForMember(d => d.ProcessingStatus, opt => opt.MapFrom(s => (int)s.ProcessingStatus));
    }
}

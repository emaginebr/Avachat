using AutoMapper;
using AvaBot.Domain.Models;
using AvaBot.DTO;

namespace AvaBot.Application.Profiles;

public class ChatSessionProfile : Profile
{
    public ChatSessionProfile()
    {
        CreateMap<ChatSession, ChatSessionInfo>()
            .ForMember(d => d.MessageCount, opt => opt.Ignore());

        CreateMap<ChatSession, ChatSessionResumeInfo>()
            .ForMember(d => d.MessageCount, opt => opt.Ignore())
            .ForMember(d => d.Messages, opt => opt.Ignore());
    }
}

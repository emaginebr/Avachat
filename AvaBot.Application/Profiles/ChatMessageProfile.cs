using AutoMapper;
using AvaBot.Domain.Models;
using AvaBot.DTO;

namespace AvaBot.Application.Profiles;

public class ChatMessageProfile : Profile
{
    public ChatMessageProfile()
    {
        CreateMap<ChatMessage, ChatMessageInfo>()
            .ForMember(d => d.SenderType, opt => opt.MapFrom(s => (int)s.SenderType));
    }
}

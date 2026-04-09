namespace SupportDeskPro.Contracts.Tickets;

public record AIDraftReplyRequest(Guid TicketId, bool IsInternal);
public record AIDraftReplyResponse(string DraftReply);
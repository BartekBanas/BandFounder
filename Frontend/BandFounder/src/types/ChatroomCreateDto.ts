export interface ChatRoomCreateDto {
    chatRoomType: string;
    name?: string;
    invitedAccountId: string;
}
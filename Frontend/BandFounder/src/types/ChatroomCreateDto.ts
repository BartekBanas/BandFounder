export interface ChatRoomCreateDto {
    chatRoomType: string;
    name?: string;
    invitedAccountId: string;
}

export interface ChatroomGroupCreateDto {
    chatRoomType: string;
    name: string;
    invitedAccountIds: string[];
}
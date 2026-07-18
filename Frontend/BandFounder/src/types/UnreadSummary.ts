export interface ChatroomUnread {
    chatRoomId: string;
    unreadCount: number;
}

export interface UnreadSummary {
    totalUnread: number;
    rooms: ChatroomUnread[];
}

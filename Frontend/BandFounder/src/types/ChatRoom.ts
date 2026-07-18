export interface ChatRoom {
    id: string;
    type: ChatRoomType
    name: string;
    ownerId?: string;
    membersIds: string[];
    lastMessageSentDate?: string | null;
    unreadCount?: number;
}

export enum ChatRoomType {
    Direct = 'Direct',
    General = 'General'
}
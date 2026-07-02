export interface ChatRoom {
    id: string;
    type: ChatRoomType
    name: string;
    ownerId?: string;
    membersIds: string[];
}

export enum ChatRoomType {
    Direct = 'Direct',
    General = 'General'
}
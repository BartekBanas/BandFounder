import {API_URL} from "../config";
import {ChatRoom, ChatRoomType} from "../types/ChatRoom";
import {ChatRoomCreateDto} from "../types/ChatroomCreateDto";
import {mantineErrorNotification} from "../components/common/mantineNotification";
import {authorizedHeaders} from "../hooks/authentication";

export async function getMyChatrooms(): Promise<ChatRoom[]> {
    try {
        const response = await fetch(`${API_URL}/chatrooms`, {
            method: 'GET',
            headers: authorizedHeaders()
        });

        if (!response.ok) {
            throw new Error(await response.text());
        }

        let chatrooms: ChatRoom[] = await response.json();
        return chatrooms;
    } catch (e) {
        mantineErrorNotification('Failed to fetch conversations');
        throw e;
    }
}

export async function getChatroom(chatroomId: string): Promise<ChatRoom> {
    try {
        const response = await fetch(`${API_URL}/chatrooms/${chatroomId}`, {
            method: 'GET',
            headers: authorizedHeaders()
        });

        if (!response.ok) {
            throw new Error(await response.text());
        }

        return await response.json() as ChatRoom;
    } catch (e) {
        mantineErrorNotification('Failed to fetch chatroom');
        throw e;
    }
}

export async function getDirectChatroomWithUser(accountId: string): Promise<ChatRoom | null> {
    try {
        const queryParams = new URLSearchParams();
        queryParams.append('ChatRoomType', ChatRoomType.Direct);
        queryParams.append('WithUser', accountId);

        const response = await fetch(`${API_URL}/chatrooms?${queryParams.toString()}`, {
            method: 'GET',
            headers: authorizedHeaders()
        });

        if (!response.ok) {
            throw new Error(await response.text());
        }

        const chatrooms = (await response.json()) as ChatRoom[];

        if (!Array.isArray(chatrooms) || chatrooms.length === 0) {
            return null;
        }

        return chatrooms[0];

    } catch (e) {
        mantineErrorNotification('Failed to fetch direct conversation');
        throw e;
    }
}

export async function createDirectChatroom(accountId: string): Promise<ChatRoom> {
    try {
        const chatRoom: ChatRoomCreateDto = {
            chatRoomType: 'Direct',
            invitedAccountId: accountId
        };

        const response = await fetch(`${API_URL}/chatrooms`, {
            method: 'POST',
            headers: authorizedHeaders(),
            body: JSON.stringify(chatRoom)
        });

        if (!response.ok) {
            const errorData = await response.text();
            throw {status: response.status, message: errorData};
        }

        return await response.json();
    } catch (e) {
        console.error('Error creating chatroom:', e);
        throw e;
    }
}

export const leaveChatroom = async (chatRoomId: string): Promise<any> => {
    try {
        const response = await fetch(`${API_URL}/chatrooms/${chatRoomId}/leave`, {
            method: 'POST',
            headers: authorizedHeaders()
        });
        return await response.text();
    } catch (e) {
        mantineErrorNotification('Failed to leave chatroom');
        console.error('Error leaving chatroom:', e);
    }
}
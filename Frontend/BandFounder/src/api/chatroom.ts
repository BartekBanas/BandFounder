import {API_URL} from "../config";
import {ChatRoom, ChatRoomType} from "../types/ChatRoom";
import {ChatroomCreate} from "../types/ChatroomCreate";
import {mantineErrorNotification} from "../components/common/mantineNotification";
import {authorizedHeaders} from "../hooks/authentication";

type DirectChatroomStarter = () => Promise<ChatRoom | null | undefined>;
type ChatroomRedirect = (chatroomId: string) => void;

export function getChatroomDestination(chatroomId: string): string {
    return `/messages/${chatroomId}`;
}

/** Prefer injecting React Router's navigate; default keeps a hard redirect for non-React callers. */
export function redirectToChatroom(chatroomId: string): void {
    window.location.assign(getChatroomDestination(chatroomId));
}

export async function openDirectChatroomWithFallback(
    accountId: string,
    startChatroom: DirectChatroomStarter = () => createDirectChatroom(accountId),
    redirect: ChatroomRedirect = redirectToChatroom
): Promise<void> {
    let chatroom: ChatRoom | null | undefined;

    try {
        chatroom = await startChatroom();
    } catch (error) {
        console.error('Error opening direct chatroom:', error);
    }

    if (!chatroom?.id) {
        chatroom = await getDirectChatroomWithUser(accountId);
    }

    if (!chatroom?.id) {
        throw new Error(`Failed to find chatroom with user ${accountId}`);
    }

    redirect(chatroom.id);
}

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
            const error = new Error(await response.text());
            Object.assign(error, {status: response.status});
            throw error;
        }

        return await response.json() as ChatRoom;
    } catch (e) {
        if ((e as {status?: number}).status !== 404) {
            mantineErrorNotification('Failed to fetch chatroom');
        }
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
        const chatRoom: ChatroomCreate = {
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

export async function createGroupChatroom(name: string): Promise<ChatRoom> {
    try {
        const chatRoom: ChatroomCreate = {
            chatRoomType: 'General',
            name: name
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
        console.error('Error creating group chatroom:', e);
        throw e;
    }
}

export async function inviteToChatroom(chatRoomId: string, userId: string): Promise<void> {
    try {
        const response = await fetch(`${API_URL}/chatrooms/${chatRoomId}/invite/${userId}`, {
            method: 'POST',
            headers: authorizedHeaders()
        });

        if (!response.ok) {
            throw new Error(await response.text());
        }
    } catch (e) {
        mantineErrorNotification('Failed to invite user to chatroom');
        throw e;
    }
}

export async function deleteChatroom(chatRoomId: string): Promise<void> {
    try {
        const response = await fetch(`${API_URL}/chatrooms/${chatRoomId}`, {
            method: 'DELETE',
            headers: authorizedHeaders()
        });

        if (!response.ok) {
            throw new Error(await response.text());
        }
    } catch (e) {
        mantineErrorNotification('Failed to delete chatroom');
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
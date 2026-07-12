import {afterEach, describe, expect, it, vi} from 'vitest';
import {getChatroomDestination, openDirectChatroomWithFallback} from './chatroom';
import {ChatRoom, ChatRoomType} from '../types/ChatRoom';

const fallbackChatroom: ChatRoom = {
    id: 'existing-chatroom',
    type: ChatRoomType.Direct,
    name: 'Direct conversation',
    membersIds: ['current-user', 'other-user'],
};

describe('openDirectChatroomWithFallback', () => {
    afterEach(() => {
        vi.restoreAllMocks();
        document.cookie = 'auth_token=; Max-Age=0';
    });

    it('uses the starter response and redirects to its chatroom', async () => {
        const redirect = vi.fn();
        const startChatroom = vi.fn().mockResolvedValue({...fallbackChatroom, id: 'new-chatroom'});

        await openDirectChatroomWithFallback('other-user', startChatroom, redirect);

        expect(startChatroom).toHaveBeenCalledOnce();
        expect(redirect).toHaveBeenCalledWith('new-chatroom');
    });

    it('finds an existing direct chatroom when the starter fails', async () => {
        document.cookie = 'auth_token=test-token';
        const redirect = vi.fn();
        const startChatroom = vi.fn().mockRejectedValue(new Error('Chatroom already exists'));
        const fetchMock = vi.spyOn(globalThis, 'fetch').mockResolvedValue(
            new Response(JSON.stringify([fallbackChatroom]), {status: 200})
        );
        vi.spyOn(console, 'error').mockImplementation(() => undefined);

        await openDirectChatroomWithFallback('other-user', startChatroom, redirect);

        expect(fetchMock).toHaveBeenCalledOnce();
        expect(redirect).toHaveBeenCalledWith('existing-chatroom');
    });
});

describe('getChatroomDestination', () => {
    it('builds the hard-navigation destination for a chatroom', () => {
        expect(getChatroomDestination('chatroom-1')).toBe('/messages/chatroom-1');
    });
});

import React, {
    createContext,
    FC,
    ReactNode,
    useCallback,
    useContext,
    useEffect,
    useMemo,
    useRef,
    useState,
} from 'react';
import {getUnreadSummary} from '../api/chatroom';

const POLL_INTERVAL_MS = 30_000;

interface UnreadMessagesContextValue {
    totalUnread: number;
    unreadByRoom: Record<string, number>;
    refresh: () => Promise<void>;
    clearRoom: (chatRoomId: string) => void;
    bumpRoom: (chatRoomId: string, amount?: number) => void;
}

const UnreadMessagesContext = createContext<UnreadMessagesContextValue | null>(null);

function summaryToMap(rooms: { chatRoomId: string; unreadCount: number }[]): Record<string, number> {
    const map: Record<string, number> = {};
    for (const room of rooms) {
        if (room.unreadCount > 0) {
            map[room.chatRoomId] = room.unreadCount;
        }
    }
    return map;
}

export const UnreadMessagesProvider: FC<{ children: ReactNode }> = ({children}) => {
    const [unreadByRoom, setUnreadByRoom] = useState<Record<string, number>>({});
    const mountedRef = useRef(true);

    const totalUnread = useMemo(
        () => Object.values(unreadByRoom).reduce((sum, count) => sum + count, 0),
        [unreadByRoom]
    );

    const refresh = useCallback(async () => {
        try {
            const summary = await getUnreadSummary();
            if (!mountedRef.current) {
                return;
            }
            setUnreadByRoom(summaryToMap(summary.rooms));
        } catch (error) {
            console.error('Failed to refresh unread summary:', error);
        }
    }, []);

    const clearRoom = useCallback((chatRoomId: string) => {
        setUnreadByRoom((prev) => {
            if (!prev[chatRoomId]) {
                return prev;
            }
            const next = {...prev};
            delete next[chatRoomId];
            return next;
        });
    }, []);

    const bumpRoom = useCallback((chatRoomId: string, amount = 1) => {
        if (amount <= 0) {
            return;
        }
        setUnreadByRoom((prev) => ({
            ...prev,
            [chatRoomId]: (prev[chatRoomId] ?? 0) + amount,
        }));
    }, []);

    useEffect(() => {
        mountedRef.current = true;
        void refresh();

        const onFocusOrVisible = () => {
            if (document.visibilityState === 'visible') {
                void refresh();
            }
        };

        window.addEventListener('focus', onFocusOrVisible);
        document.addEventListener('visibilitychange', onFocusOrVisible);

        const intervalId = window.setInterval(() => {
            if (document.visibilityState === 'visible') {
                void refresh();
            }
        }, POLL_INTERVAL_MS);

        return () => {
            mountedRef.current = false;
            window.removeEventListener('focus', onFocusOrVisible);
            document.removeEventListener('visibilitychange', onFocusOrVisible);
            window.clearInterval(intervalId);
        };
    }, [refresh]);

    const value = useMemo(
        () => ({totalUnread, unreadByRoom, refresh, clearRoom, bumpRoom}),
        [totalUnread, unreadByRoom, refresh, clearRoom, bumpRoom]
    );

    return (
        <UnreadMessagesContext.Provider value={value}>
            {children}
        </UnreadMessagesContext.Provider>
    );
};

export function useUnreadMessages(): UnreadMessagesContextValue {
    const context = useContext(UnreadMessagesContext);
    if (!context) {
        throw new Error('useUnreadMessages must be used within UnreadMessagesProvider');
    }
    return context;
}

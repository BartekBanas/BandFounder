import React, {FC, memo, useCallback, useEffect, useLayoutEffect, useMemo, useRef, useState} from "react";
import {useNavigate} from "react-router-dom";
import {ChatRoom, ChatRoomType} from "../../../types/ChatRoom";
import {createDirectChatroom, getChatroomDestination, getMyChatrooms} from "../../../api/chatroom";
import {Autocomplete, Avatar, CircularProgress, TextField} from "@mui/material";
import GroupsIcon from "@mui/icons-material/Groups";
import './styles.css'
import '../../../styles/customScrollbar.css'
import {getUserId} from "../../../hooks/authentication";
import {getAccounts} from "../../../api/account";
import {Account} from "../../../types/Account";
import {mantineErrorNotification} from "../../common/mantineNotification";
import {LeaveChatroomModal} from "./LeaveChatroomModal";
import {CreateGroupChatroomModal} from "./CreateGroupChatroomModal";
import UserAvatar from "../../common/UserAvatar";

interface AllConversationsProps {
    selectedId: string;
    activity?: { chatroomId: string; sentDate: string } | null;
}

function resolveDirectChatName(chatRoom: ChatRoom, myId: string, accountsById: Map<string, Account>): string {
    const otherUserId = chatRoom.membersIds.find((id) => id !== myId);
    if (!otherUserId) {
        return 'Unknown';
    }
    return accountsById.get(otherUserId)?.name ?? 'Unknown';
}

function withDisplayNames(chatrooms: ChatRoom[], myId: string, accountsById: Map<string, Account>): ChatRoom[] {
    return chatrooms.map((chatroom) => {
        if (chatroom.type !== ChatRoomType.Direct) {
            return chatroom;
        }

        const otherUserId = chatroom.membersIds.find((id) => id !== myId);
        return {
            ...chatroom,
            name: resolveDirectChatName(chatroom, myId, accountsById),
            membersIds: otherUserId ? [otherUserId] : chatroom.membersIds,
        };
    });
}

function lastMessageTime(chatRoom: ChatRoom): number {
    if (!chatRoom.lastMessageSentDate) {
        return 0;
    }
    const parsed = Date.parse(chatRoom.lastMessageSentDate);
    return Number.isNaN(parsed) ? 0 : parsed;
}

function sortByLatestMessage(chatrooms: ChatRoom[]): ChatRoom[] {
    return [...chatrooms].sort((a, b) => lastMessageTime(b) - lastMessageTime(a));
}

const ChatroomAvatar = memo(function ChatroomAvatar({chatRoom, myId}: { chatRoom: ChatRoom, myId: string }) {
    switch (chatRoom.type) {
        case ChatRoomType.Direct: {
            const otherUserId = chatRoom.membersIds.find(id => id !== myId) ?? chatRoom.membersIds[0];
            return <UserAvatar userId={otherUserId} size={48}/>;
        }

        case ChatRoomType.General:
            return (
                <Avatar sx={{width: 48, height: 48}}>
                    <GroupsIcon/>
                </Avatar>
            );

        default:
            throw new Error(`Unsupported chatroom type '${chatRoom.type}'`);
    }
});

interface ConversationListItemProps {
    chatRoom: ChatRoom;
    myId: string;
    isSelected: boolean;
    onSelect: (id: string) => void;
    onLeft: (id: string) => void;
}

const ConversationListItem = memo(function ConversationListItem({
    chatRoom,
    myId,
    isSelected,
    onSelect,
    onLeft,
}: ConversationListItemProps) {
    return (
        <li
            className={`singleConversationShortcut${isSelected ? ' active' : ''}`}
            data-chatroom-id={chatRoom.id}
            onClick={() => onSelect(chatRoom.id)}
        >
            <ChatroomAvatar chatRoom={chatRoom} myId={myId}/>
            <div className={'conversationName'}>{chatRoom.name}</div>
            <div className={'leaveChatroomBtn'} onClick={(e) => e.stopPropagation()}>
                <LeaveChatroomModal chatroom={chatRoom} onLeft={onLeft}/>
            </div>
        </li>
    );
});

export const AllConversations: FC<AllConversationsProps> = ({selectedId, activity}) => {
    const navigate = useNavigate();
    const [chatRooms, setChatRooms] = useState<ChatRoom[]>([]);
    const [otherUsers, setOtherUsers] = useState<Account[]>([]);
    const [loading, setLoading] = useState(true);
    const myId = getUserId();
    const listRef = useRef<HTMLUListElement>(null);
    const previousTopsRef = useRef<Map<string, number>>(new Map());
    const pendingFlipRef = useRef(false);
    const chatRoomsRef = useRef<ChatRoom[]>(chatRooms);
    chatRoomsRef.current = chatRooms;

    const userNameOptions = useMemo(() => otherUsers.map((user) => user.name), [otherUsers]);

    const snapshotListPositions = useCallback(() => {
        const list = listRef.current;
        if (!list) {
            return;
        }

        const listTop = list.getBoundingClientRect().top;
        const tops = new Map<string, number>();
        list.querySelectorAll<HTMLElement>('[data-chatroom-id]').forEach((item) => {
            item.getAnimations().forEach((animation) => animation.cancel());
            item.style.transition = 'none';
            item.style.transform = '';
            const id = item.dataset.chatroomId;
            if (id) {
                tops.set(id, item.getBoundingClientRect().top - listTop + list.scrollTop);
            }
        });
        previousTopsRef.current = tops;
    }, []);

    const fetchConversations = useCallback(async () => {
        setLoading(true);
        try {
            const [chatrooms, accounts] = await Promise.all([getMyChatrooms(), getAccounts()]);
            const accountsById = new Map(accounts.map((account) => [account.id, account]));
            setOtherUsers(accounts.filter((account) => account.id !== myId));
            pendingFlipRef.current = false;
            previousTopsRef.current = new Map();
            setChatRooms(sortByLatestMessage(withDisplayNames(chatrooms, myId, accountsById)));
        } catch (error) {
            console.error('Error fetching conversations:', error);
        } finally {
            setLoading(false);
        }
    }, [myId]);

    useEffect(() => {
        fetchConversations();
    }, [fetchConversations]);

    useEffect(() => {
        if (!activity) {
            return;
        }

        const prev = chatRoomsRef.current;
        const existing = prev.find((chatRoom) => chatRoom.id === activity.chatroomId);
        if (!existing) {
            return;
        }

        const currentTime = lastMessageTime(existing);
        const nextTime = Date.parse(activity.sentDate);
        if (!Number.isNaN(nextTime) && nextTime <= currentTime) {
            return;
        }

        const nextRooms = sortByLatestMessage(
            prev.map((chatRoom) =>
                chatRoom.id === activity.chatroomId
                    ? {...chatRoom, lastMessageSentDate: activity.sentDate}
                    : chatRoom
            )
        );

        const orderChanged = nextRooms.some((chatRoom, index) => chatRoom.id !== prev[index]?.id);
        if (orderChanged) {
            snapshotListPositions();
            pendingFlipRef.current = true;
        }

        setChatRooms(nextRooms);
    }, [activity, snapshotListPositions]);

    useLayoutEffect(() => {
        const list = listRef.current;
        if (!list || !pendingFlipRef.current) {
            return;
        }

        pendingFlipRef.current = false;
        const previousTops = previousTopsRef.current;
        const listTop = list.getBoundingClientRect().top;

        list.querySelectorAll<HTMLElement>('[data-chatroom-id]').forEach((item) => {
            const id = item.dataset.chatroomId;
            if (!id) {
                return;
            }

            const previousTop = previousTops.get(id);
            if (previousTop === undefined) {
                return;
            }

            const nextTop = item.getBoundingClientRect().top - listTop + list.scrollTop;
            const deltaY = previousTop - nextTop;
            if (Math.abs(deltaY) < 1) {
                return;
            }

            item.style.transition = 'none';
            item.style.transform = `translateY(${deltaY}px)`;
            void item.offsetHeight;
            item.style.transition = 'transform 0.38s cubic-bezier(0.22, 1, 0.36, 1)';
            item.style.transform = '';
        });

        const clearInlineStyles = () => {
            list.querySelectorAll<HTMLElement>('[data-chatroom-id]').forEach((item) => {
                item.style.transition = '';
                item.style.transform = '';
            });
        };

        const timeoutId = window.setTimeout(clearInlineStyles, 420);
        return () => {
            window.clearTimeout(timeoutId);
        };
    }, [chatRooms]);

    const findChatroomIdByUserName = useCallback((userName: string): string => {
        const match = chatRooms.find(
            (chatRoom) => chatRoom.name.toLowerCase() === userName.toLowerCase()
        );
        return match?.id ?? '';
    }, [chatRooms]);

    const handleSelectConversation = useCallback((id: string) => {
        navigate(getChatroomDestination(id));
    }, [navigate]);

    const handleConversationLeft = useCallback((leftId: string) => {
        setChatRooms((prev) => prev.filter((chatRoom) => chatRoom.id !== leftId));
        if (selectedId === leftId) {
            navigate('/messages');
        }
    }, [navigate, selectedId]);

    const handleConversationCreated = useCallback((id: string) => {
        fetchConversations().then(() => {
            navigate(getChatroomDestination(id));
        });
    }, [fetchConversations, navigate]);

    const handleCreateChatroom = async (userName: string | null) => {
        if (!userName) return;

        try {
            const userId = otherUsers.find((user) => user.name === userName)?.id;
            if (typeof userId === "string") {
                const newChatRoom = await createDirectChatroom(userId);
                handleConversationCreated(newChatRoom.id);
            } else {
                throw new Error('User not found');
            }
        } catch (error: any) {
            if (error.status === 409) {
                const chatroomId = findChatroomIdByUserName(userName);
                if (chatroomId) {
                    navigate(getChatroomDestination(chatroomId));
                } else {
                    await fetchConversations();
                    mantineErrorNotification('Conversation already exists — try selecting it from the list');
                }
            } else {
                mantineErrorNotification('Failed to create chatroom with ' + userName);
                console.error("Error creating chatroom:", error);
            }
        }
    };

    return (
        <div id="mainChatroomsList">
            <div id="chatroomListActions">
                <Autocomplete
                    options={userNameOptions}
                    onChange={(event, newValue) => {
                        handleCreateChatroom(newValue);
                    }}
                    renderInput={(params) => <TextField {...params} label="Search Users" variant="outlined"/>}
                    style={{flex: 1}}
                />
                <CreateGroupChatroomModal
                    otherUsers={otherUsers}
                    onCreated={handleConversationCreated}
                />
            </div>
            {loading ? (
                <div style={{display: 'flex', justifyContent: 'center', alignItems: 'center'}}>
                    <CircularProgress size={30} sx={{marginBottom: '20px'}}/>
                </div>
            ) : chatRooms.length > 0 ? (
                <div id="chatroomsListHeading">Open Conversations</div>
            ) : (
                <div id="chatroomsListHeading">No conversations yet</div>
            )}
            <ul id={'openConversationsList'} className={'custom-scrollbar'} ref={listRef}>
                {!loading && chatRooms.map((chatRoom) => (
                    <ConversationListItem
                        key={chatRoom.id}
                        chatRoom={chatRoom}
                        myId={myId}
                        isSelected={chatRoom.id === selectedId}
                        onSelect={handleSelectConversation}
                        onLeft={handleConversationLeft}
                    />
                ))}
            </ul>
        </div>
    );
};

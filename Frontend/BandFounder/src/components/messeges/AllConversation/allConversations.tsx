import React, {FC, memo, useCallback, useEffect, useMemo, useState} from "react";
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

export const AllConversations: FC<AllConversationsProps> = ({selectedId}) => {
    const navigate = useNavigate();
    const [chatRooms, setChatRooms] = useState<ChatRoom[]>([]);
    const [otherUsers, setOtherUsers] = useState<Account[]>([]);
    const [loading, setLoading] = useState(true);
    const myId = getUserId();

    const userNameOptions = useMemo(() => otherUsers.map((user) => user.name), [otherUsers]);

    const fetchConversations = useCallback(async () => {
        setLoading(true);
        try {
            const [chatrooms, accounts] = await Promise.all([getMyChatrooms(), getAccounts()]);
            const accountsById = new Map(accounts.map((account) => [account.id, account]));
            setOtherUsers(accounts.filter((account) => account.id !== myId));
            setChatRooms(withDisplayNames(chatrooms, myId, accountsById));
        } catch (error) {
            console.error('Error fetching conversations:', error);
        } finally {
            setLoading(false);
        }
    }, [myId]);

    useEffect(() => {
        fetchConversations();
    }, [fetchConversations]);

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
            <ul id={'openConversationsList'} className={'custom-scrollbar'}>
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
import React, {FC, useEffect, useRef, useState} from "react";
import {Message} from "../../../types/Message";
import {IconButton, TextField, Tooltip} from "@mui/material";
import SendIcon from "@mui/icons-material/Send";
import "./styles.css";
import "../../../styles/customScrollbar.css";
import {getMessagesFromChatroom, sendMessage} from "../../../api/messages";
import {getAccount} from "../../../api/account";
import {getUserId} from "../../../hooks/authentication";
import {Account} from "../../../types/Account";
import {ChatRoom, ChatRoomType} from "../../../types/ChatRoom";
import {getChatroom} from "../../../api/chatroom";
import UserAvatar from "../../common/UserAvatar";
import {formatMessageWithLinks} from "../../common/utils";

interface SelectedConversationProps {
    id: string;
}

interface MessageWithSenderName extends Message {
    senderName: string;
    formattedSentDate: string;
}

const parseMessageDate = (sentDate: string | undefined): Date => {
    if (!sentDate) return new Date();
    const date = new Date(sentDate);
    return Number.isNaN(date.getTime()) ? new Date() : date;
};

const formatMessageDate = (messageTime: Date): string => {
    const day = messageTime.getDate().toString().padStart(2, "0");
    const month = (messageTime.getMonth() + 1).toString().padStart(2, "0");
    const year = messageTime.getFullYear();
    const hours = messageTime.getHours().toString().padStart(2, "0");
    const minutes = messageTime.getMinutes().toString().padStart(2, "0");
    return `${day}/${month}/${year} ${hours}:${minutes}`;
};

const formatMessageTime = (messageTime: Date): string => {
    const hours = messageTime.getHours().toString().padStart(2, "0");
    const minutes = messageTime.getMinutes().toString().padStart(2, "0");
    return `${hours}:${minutes}`;
};

const isSameDay = (a: Date, b: Date): boolean =>
    a.getFullYear() === b.getFullYear() &&
    a.getMonth() === b.getMonth() &&
    a.getDate() === b.getDate();

export const SelectedConversation: FC<SelectedConversationProps> = ({id}) => {
    const [chatroom, setChatroom] = useState<ChatRoom>();
    const [currentConversation, setCurrentConversation] = useState<MessageWithSenderName[]>([]);
    const [newMessage, setNewMessage] = useState<string>("");
    const [chatroomName, setChatroomName] = useState<string>("");
    const [loading, setLoading] = useState<boolean>(false);
    const [pageNumber, setPageNumber] = useState<number>(1);
    const [pageSize] = useState<number>(10);
    const [hasMore, setHasMore] = useState<boolean>(true);
    const bottomRef = useRef<HTMLDivElement>(null);
    const ws = useRef<WebSocket | null>(null);
    const userCache = useRef<Record<string, string>>({}); // Cache for sender names

    const [participants, setParticipants] = useState<Account[]>([]);
    const addParticipant = (account: Account) => {
        setParticipants((prevParticipants) => {
            if (prevParticipants.some((participant) => participant.id === account.id)) {
                return prevParticipants;
            }
            return [...prevParticipants, account];
        });
    };

    useEffect(() => {
        bottomRef.current?.scrollIntoView({behavior: "smooth"});
    }, [currentConversation]);

    useEffect(() => {
        if (!id) return;

        const fetchChatroom = async () => {
            const chatroom = await getChatroom(id);
            if (chatroom.membersIds) {
                const localParticipants: Account[] = [];

                for (const memberId of chatroom.membersIds) {
                    const member = await getAccount(memberId);
                    localParticipants.push(member);
                    addParticipant(member);
                }

                if (chatroom.type === ChatRoomType.Direct) {
                    const otherParticipant = localParticipants.find(
                        (participant) => participant.id !== getUserId()
                    );
                    const otherParticipantName = otherParticipant ? otherParticipant.name : "Unknown User";
                    setChatroomName(`Conversation with ${otherParticipantName}`);
                } else {
                    setChatroomName(chatroom.name || "Unknown Chatroom");
                }
            }

            setChatroom(chatroom);
        };

        fetchChatroom();
    }, [id]);

    useEffect(() => {
        if (!id) {
            console.error("Chatroom ID is not provided");
            return;
        }

        const initializeChatroom = async () => {
            await fetchOlderMessages(); // Fetch initial messages
            bottomRef.current?.scrollIntoView({behavior: "smooth"}); // Scroll to bottom after fetching messages
        };

        const wsUrl = `wss://localhost:7095/api/chatrooms?chatRoomId=${id}`;
        ws.current = new WebSocket(wsUrl);

        ws.current.onmessage = async (event) => {
            try {
                const message = JSON.parse(event.data);
                console.log("Received message:", message);
                if (!message.senderId) return;

                let account = participants.find((participant) => participant.id === message.senderId);
                if (!account) {
                    account = await getAccount(message.senderId);
                    addParticipant(account);
                }

                const senderName = account.name;
                const formattedSentDate = formatMessageDate(parseMessageDate(message.sentDate));

                const newMessageWithSenderName: MessageWithSenderName = {
                    ...message,
                    senderName,
                    formattedSentDate,
                };
                setCurrentConversation((prev) => [...prev, newMessageWithSenderName]);
                bottomRef.current?.scrollIntoView({behavior: "smooth"}); // Scroll to bottom after receiving a new message
            } catch (error) {
                console.error("Error parsing WebSocket message:", error);
            }
        };

        ws.current.onclose = () => {
            console.log("WebSocket disconnected");
        };

        initializeChatroom(); // Trigger initial message load

        return () => {
            ws.current?.close();
        };
    }, [id]);


    const fetchOlderMessages = async () => {
        if (!id || !hasMore || loading) {
            console.error("Chatroom ID is not provided or no more messages to fetch or already loading");
            return;
        }

        setLoading(true);
        try {
            const conversation: Message[] = await getMessagesFromChatroom(id, pageNumber, pageSize);

            if (conversation.length < pageSize) setHasMore(false);

            const conversationWithNames = await Promise.all(
                conversation.map(async (message) => {
                    let senderName: string;
                    if (userCache.current[message.senderId]) {
                        senderName = userCache.current[message.senderId];
                    } else {
                        try {
                            const user = await getAccount(message.senderId);
                            senderName = user.name;
                            userCache.current[message.senderId] = senderName;
                        } catch (error) {
                            senderName = "Deleted User";
                            userCache.current[message.senderId] = senderName;
                        }
                    }

                    return {
                        ...message,
                        senderName,
                        formattedSentDate: formatMessageDate(parseMessageDate(message.sentDate)),
                    };
                })
            );

            setCurrentConversation((prev) => [
                ...conversationWithNames.reverse(),
                ...prev,
            ]);
            setPageNumber((prev) => prev + 1);
        } catch (error) {
            console.error("Error fetching older messages:", error);
        } finally {
            setLoading(false);
        }
    };


    useEffect(() => {
        if (!id) return;

        const conversationElement = document.getElementById("fullConversation");

        if (!conversationElement) {
            console.error("Conversation element not found");
            return;
        }

        const handleScroll = async () => {
            if (conversationElement.scrollTop === 0 && hasMore && !loading) {
                const currentScrollHeight = conversationElement.scrollHeight;
                await fetchOlderMessages();
                setTimeout(() => {
                    if (conversationElement.scrollHeight > currentScrollHeight) {
                        conversationElement.scrollTop =
                            conversationElement.scrollHeight - currentScrollHeight;
                    }
                }, 0);
            }
        };

        const checkIfScrollable = async () => {
            if (conversationElement.scrollHeight <= conversationElement.clientHeight && hasMore && !loading) {
                await fetchOlderMessages();
            }
        };

        conversationElement.addEventListener("scroll", handleScroll);
        checkIfScrollable();

        return () => {
            conversationElement.removeEventListener("scroll", handleScroll);
        };
    }, [hasMore, loading, currentConversation]);


    const handleSendMessage = async () => {
        if (!newMessage.trim()) return;

        try {
            const trimmedMessage = newMessage.trim();
            const userId = getUserId();
            const messageTime = new Date();

            const newMessageObject: MessageWithSenderName = {
                id: Math.random().toString(),
                senderId: userId,
                content: trimmedMessage,
                sentDate: messageTime.toISOString(),
                senderName: "You",
                formattedSentDate: formatMessageDate(messageTime),
            };

            // setCurrentConversation((prev) => [...prev, newMessageObject]);
            await sendMessage(id, trimmedMessage);

            if (ws.current?.readyState === WebSocket.OPEN) {
                ws.current.send(JSON.stringify({content: trimmedMessage}));
            }

            setNewMessage("");
        } catch (error) {
            console.error("Error sending message:", error);
        }
    };

    const handleKeyPress = (event: React.KeyboardEvent<HTMLInputElement>) => {
        if (event.key === "Enter") {
            event.preventDefault();
            handleSendMessage();
        }
    };

    if (!id) {
        return (
            <div id="mainSelectedConversation">
                <div id="emptyConversation">Select a chat to start messaging</div>
            </div>
        );
    }

    return (
        <div id="mainSelectedConversation">
            <h1 id="selectedConversationTitle">{chatroomName}</h1>
            <ul id="fullConversation" className="custom-scrollbar">
                {[...currentConversation]
                    .sort((a, b) => parseMessageDate(a.sentDate).getTime() - parseMessageDate(b.sentDate).getTime())
                    .map((message, index, sortedConversation) => {
                    const previousMessage = sortedConversation[index - 1];
                    const GROUP_TIME_GAP_MS = 5 * 60 * 1000;
                    const messageSentDate = parseMessageDate(message.sentDate);
                    const previousMessageSentDate = previousMessage
                        ? parseMessageDate(previousMessage.sentDate)
                        : null;
                    const isFirstInGroup =
                        index === 0 ||
                        previousMessage.senderId !== message.senderId ||
                        !previousMessageSentDate ||
                        !isSameDay(messageSentDate, previousMessageSentDate) ||
                        messageSentDate.getTime() - previousMessageSentDate.getTime() >
                            GROUP_TIME_GAP_MS;
                    const messageContent = (
                        <div className="messageContent">{formatMessageWithLinks(message.content)}</div>
                    );
                    return (
                        <li
                            key={index}
                            className={`messageRow${isFirstInGroup ? "" : " grouped"}`}
                        >
                            <div className="messageAvatarGutter">
                                {isFirstInGroup ? (
                                    <Tooltip title={message.senderName}>
                                        <div>
                                            <UserAvatar userId={message.senderId} size={40}/>
                                        </div>
                                    </Tooltip>
                                ) : (
                                    <span className="groupedMessageTime">
                                        {formatMessageTime(messageSentDate)}
                                    </span>
                                )}
                            </div>
                            <div className="messageBody">
                                {isFirstInGroup && (
                                    <div className="messageHeader">
                                        <span className="senderName">{message.senderName}</span>
                                        <span className="messageTime">{message.formattedSentDate}</span>
                                    </div>
                                )}
                                {messageContent}
                            </div>
                        </li>
                    );
                })}
                <div ref={bottomRef}/>
            </ul>
            <div id="sendBox">
                <TextField
                    fullWidth
                    variant="outlined"
                    label="Type a message"
                    value={newMessage}
                    onChange={(e) => setNewMessage(e.target.value)}
                    onKeyPress={handleKeyPress}
                    inputProps={{"aria-label": "Type a message"}}
                />
                <IconButton color="primary" onClick={handleSendMessage} aria-label="Send message">
                    <SendIcon/>
                </IconButton>
            </div>
        </div>
    );
};
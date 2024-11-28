import React, { FC, useEffect, useRef, useState } from "react";
import { Message } from "../../../types/Message";
import { CircularProgress, IconButton, TextField, Tooltip } from "@mui/material";
import SendIcon from "@mui/icons-material/Send";
import "./styles.css";
import "./../../../assets/CustomScrollbar.css";
import { getMessagesFromChatroom, sendMessage } from "../../../api/messages";
import { getAccount, getProfilePicture } from "../../../api/account";
import { getUserId } from "../../../hooks/authentication";
import { Account } from "../../../types/Account";
import { ChatRoom, ChatRoomType } from "../../../types/ChatRoom";
import { getChatroom } from "../../../api/chatroom";
import { ImageAvatar } from "../../common/ImageAvatar";
import UserAvatar from "../../common/UserAvatar";

interface SelectedConversationProps {
    id: string;
}

interface MessageWithSenderName extends Message {
    senderName: string;
    timeSinceSent: string;
}

export const SelectedConversation: FC<SelectedConversationProps> = ({ id }) => {
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
        bottomRef.current?.scrollIntoView({ behavior: "smooth" });
    }, [currentConversation]);

    useEffect(() => {
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
            bottomRef.current?.scrollIntoView({ behavior: "smooth" }); // Scroll to bottom after fetching messages
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
                const timeSinceSent = calculateTimeSinceSent(new Date(message.sentDate));

                const newMessageWithSenderName: MessageWithSenderName = {
                    ...message,
                    senderName,
                    timeSinceSent,
                };
                setCurrentConversation((prev) => [...prev, newMessageWithSenderName]);
                bottomRef.current?.scrollIntoView({ behavior: "smooth" }); // Scroll to bottom after receiving a new message
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
                    const senderName =
                        userCache.current[message.senderId] ??
                        (await getAccount(message.senderId)
                            .then((user) => {
                                userCache.current[message.senderId] = user.name || "Unknown User";
                                return user.name || "Unknown User";
                            })
                            .catch(() => "Unknown User"));

                    return {
                        ...message,
                        senderName,
                        timeSinceSent: calculateTimeSinceSent(new Date(message.sentDate)),
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
        const conversationElement = document.getElementById("fullConversation");

        if (!conversationElement) {
            console.error("Conversation element not found"); // Debug
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

        conversationElement.addEventListener("scroll", handleScroll);
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
                timeSinceSent: "just now",
            };

            // setCurrentConversation((prev) => [...prev, newMessageObject]);
            await sendMessage(id, trimmedMessage);

            if (ws.current?.readyState === WebSocket.OPEN) {
                ws.current.send(JSON.stringify({ content: trimmedMessage }));
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

    const calculateTimeSinceSent = (messageTime: Date): string => {
        const currentTime = new Date();
        const timeDifference = Math.floor((currentTime.getTime() - messageTime.getTime()) / 1000);

        if (timeDifference < 60) return `${timeDifference} second${timeDifference === 1 ? "" : "s"} ago`;
        if (timeDifference < 3600) return `${Math.floor(timeDifference / 60)} minute${Math.floor(timeDifference / 60) === 1 ? "" : "s"} ago`;
        if (timeDifference < 86400) return `${Math.floor(timeDifference / 3600)} hour${Math.floor(timeDifference / 3600) === 1 ? "" : "s"} ago`;
        return `${Math.floor(timeDifference / 86400)} day${Math.floor(timeDifference / 86400) === 1 ? "" : "s"} ago`;
    };

    return (
        <div id="mainSelectedConversation">
            <h1 id="selectedConversationTitle">{chatroomName}</h1>
            <ul id="fullConversation" className="custom-scrollbar" style={{ listStyleType: "none", padding: 0 }}>
                {currentConversation.map((message, index) => (
                    <li
                        key={index}
                        style={{
                            textAlign: message.senderName === "You" ? "right" : "left",
                            margin: "10px 0",
                        }}
                    >
                        {message.senderId === getUserId() ? (
                            <div className="singleMessageYou">
                                <Tooltip title={message.senderName}>
                                    <div style={{ display: 'flex', justifyContent: 'center', alignItems: 'center', marginLeft: '3px' }}>
                                        <UserAvatar userId={message.senderId} size={20} style={{display:'flex', alignItems:'center'}}/>
                                    </div>
                                </Tooltip>
                                <Tooltip title={`Sent ${message.timeSinceSent}`}>
                                    <div className="messageContent">{message.content}</div>
                                </Tooltip>
                            </div>
                        ) : (
                            <div className="singleMessageThey">
                                <Tooltip title={message.senderName}>
                                    <div style={{ display: 'flex', justifyContent: 'center', alignItems: 'center', marginRight: '3px' }}>
                                        <UserAvatar userId={message.senderId} size={20} style={{display:'flex', alignItems:'center'}}/>
                                    </div>
                                </Tooltip>
                                <Tooltip title={`Sent ${message.timeSinceSent}`}>
                                    <div className="messageContent">{message.content}</div>
                                </Tooltip>
                            </div>
                        )}
                    </li>
                ))}
                <div ref={bottomRef} />
            </ul>
            <div id="sendBox" style={{ display: "flex", alignItems: "center", marginTop: "1rem" }}>
                <TextField
                    fullWidth
                    variant="outlined"
                    label="Type a message"
                    value={newMessage}
                    onChange={(e) => setNewMessage(e.target.value)}
                    onKeyPress={handleKeyPress}
                    inputProps={{ "aria-label": "Type a message" }}
                />
                <IconButton color="primary" onClick={handleSendMessage} aria-label="Send message">
                    <SendIcon />
                </IconButton>
            </div>
        </div>
    );
};

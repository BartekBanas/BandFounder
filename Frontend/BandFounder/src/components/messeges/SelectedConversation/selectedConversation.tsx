import { FC, useEffect, useRef, useState } from "react";
import { Message } from "../../../types/Message";
import { TextField, IconButton, Tooltip, CircularProgress } from "@mui/material";
import SendIcon from "@mui/icons-material/Send";
import "./styles.css";
import defaultProfileImage from "../../../assets/defaultProfileImage.jpg";
import "./../../../assets/CustomScrollbar.css";
import { getMessagesFromChatroom, sendMessage } from "../../../api/messages";
import {getAccount, getMyAccount} from "../../../api/account";
import { getUserId } from "../../../hooks/authentication";
import {Account} from "../../../types/Account";

interface SelectedConversationProps {
    id: string;
}

interface MessageWithSenderName extends Message {
    senderName: string;
    timeSinceSent: string;
}

export const SelectedConversation: FC<SelectedConversationProps> = ({ id }) => {
    const [currentConversation, setCurrentConversation] = useState<MessageWithSenderName[]>([]);
    const [newMessage, setNewMessage] = useState<string>("");
    const [receiverName, setReceiverName] = useState<string>("Unknown User");
    const [loading, setLoading] = useState<boolean>(false);
    const [pageNumber, setPageNumber] = useState<number>(1);
    const [pageSize] = useState<number>(5);
    const [hasMore, setHasMore] = useState<boolean>(true);
    const bottomRef = useRef<HTMLDivElement>(null);
    const topRef = useRef<HTMLDivElement>(null);
    const [isScrollingUp, setIsScrollingUp] = useState<boolean>(false);
    const ws = useRef<WebSocket | null>(null);
    const userCache = useRef<Record<string, string>>({}); // Cache for sender names
    const [currentUser, setCurrentUser] = useState<Account | undefined>();

    useEffect(() => {
        if (!id) {
            console.error("Chat room ID is not provided");
            return;
        }
        const wsUrl = `wss://localhost:7095/api/chatrooms?chatRoomId=${id}`;
        ws.current = new WebSocket(wsUrl);

        ws.current.onmessage = async (event) => {
            try {
                const message = JSON.parse(event.data);
                if (!message.senderId) {
                    console.error("Received message with undefined senderId:", message);
                    return;
                }
                console.log(message);

                // Await the promise returned by getAccount to access the name property
                const account = await getAccount(message.senderId.Id);
                const senderName = account.name;

                const timeSinceSent = calculateTimeSinceSent(new Date(message.sentDate));

                const newMessageWithSenderName: MessageWithSenderName = {
                    ...message,
                    senderName,
                    timeSinceSent,
                };
                const myId = await getMyAccount();
                if(account.id !== myId.id){
                    setCurrentConversation((prev) => [...prev, newMessageWithSenderName]);
                }
            } catch (error) {
                console.error("Error parsing WebSocket message:", error);
            }
        };

        ws.current.onclose = () => {
            console.log("WebSocket disconnected");
        };

        return () => {
            ws.current?.close();
        };
    }, [id]);

    useEffect(() => {
        const fetchOlderMessages = async () => {
            setCurrentUser(await getMyAccount());
            if (!id || !hasMore || pageNumber <= 0) return;

            setLoading(true);
            try {
                const conversation: Message[] = await getMessagesFromChatroom(id, pageNumber, pageSize);
                const userId = getUserId();

                const conversationWithNames = await Promise.all(
                    conversation.map(async (message) => {
                        if (!message.senderId) {
                            console.error("Fetched message with undefined senderId:", message);
                            return null;
                        }

                        const senderName =
                            userCache.current[message.senderId] ??
                            (await getAccount(message.senderId)
                                .then((user) => {
                                    const name = user.name || "Unknown User";
                                    userCache.current[message.senderId] = name;
                                    return name;
                                })
                                .catch((error) => {
                                    console.error("Failed to fetch user:", error);
                                    return "Unknown User";
                                }));

                        const timeSinceSent = calculateTimeSinceSent(new Date(message.sentDate));

                        return {
                            ...message,
                            senderName,
                            timeSinceSent,
                        };
                    })
                );

                const validMessages = conversationWithNames.filter((msg) => msg !== null) as MessageWithSenderName[];

                const sortedConversationWithNames = validMessages.sort(
                    (a, b) => new Date(a.sentDate).getTime() - new Date(b.sentDate).getTime()
                );

                setCurrentConversation((prev) => [...sortedConversationWithNames, ...prev]);
                setHasMore(conversation.length === pageSize); // If fetched less than `pageSize`, no more messages
                setPageNumber((prev) => prev + 1); // Increment page number for next fetch
            } catch (error) {
                console.error("Error fetching older messages:", error);
            } finally {
                setLoading(false);
            }
        };


        fetchOlderMessages();
    }, [id, hasMore, pageNumber, pageSize]);

    useEffect(() => {
        const container = topRef.current?.parentElement;
        if (bottomRef.current) {
            bottomRef.current.scrollIntoView({ behavior: "smooth" });
        }
    }, [currentConversation]);

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

            setCurrentConversation((prev) => [...prev, newMessageObject]);
            await sendMessage(id, trimmedMessage);

            if (ws.current?.readyState === WebSocket.OPEN) {
                ws.current.send(JSON.stringify({ content: trimmedMessage }));
            } else {
                console.error("WebSocket is not open");
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
            <h1 id="selectedConversationTitle">Conversation with {receiverName}</h1>
            <ul id="fullConversation" className="custom-scrollbar" style={{ listStyleType: "none", padding: 0 }}>
                {loading && <CircularProgress />}
                <div ref={topRef} />
                {currentConversation.map((message, index) => (
                    <li
                        key={index}
                        style={{
                            textAlign: message.senderName === "You" ? "right" : "left",
                            margin: "10px 0",
                        }}
                    >
                        {message.senderName === "You" || message.senderName === currentUser?.name ? (
                            <div className="singleMessageYou">
                                <Tooltip title={`${message.senderName}`}>
                                    <div className="messageProfilePresentation">
                                        <img src={defaultProfileImage} alt="defaultProfileImage" />
                                    </div>
                                </Tooltip>
                                <Tooltip title={`Sent ${message.timeSinceSent}`}>
                                    <div className="messageContent">{message.content}</div>
                                </Tooltip>
                            </div>
                        ) : (
                            <div className="singleMessageThey">
                                <Tooltip title={`${message.senderName}`}>
                                    <div className="messageProfilePresentation">
                                        <img src={defaultProfileImage} alt="defaultProfileImage" />
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
import React, {FC, useCallback, useEffect, useLayoutEffect, useRef, useState} from "react";
import {Message} from "../../../types/Message";
import {IconButton, TextField} from "@mui/material";
import SendIcon from "@mui/icons-material/Send";
import "./styles.css";
import "../../../styles/customScrollbar.css";
import {getMessagesFromChatroom, sendMessage} from "../../../api/messages";
import {getAccount} from "../../../api/account";
import {getUserId} from "../../../hooks/authentication";
import {Account} from "../../../types/Account";
import {ChatRoom, ChatRoomType} from "../../../types/ChatRoom";
import {getChatroom} from "../../../api/chatroom";
import InteractiveUserAvatar from "../../common/InteractiveUserAvatar";
import {formatMessageWithLinks} from "../../common/utils";
import {GroupMembersPanel} from "./GroupMembersPanel";
import {MissingContent} from "../../common/MissingContent";
import {AppLoader} from "../../common/AppLoader";
import {API_URL} from "../../../config";

interface SelectedConversationProps {
    id: string;
    onConversationActivity?: (chatroomId: string, sentDate: string) => void;
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

const GROUP_TIME_GAP_MS = 5 * 60 * 1000;

function buildWebSocketUrl(chatRoomId: string): string {
    const apiBase = API_URL.replace(/\/$/, "");
    const wsBase = apiBase.replace(/^http/, "ws");
    return `${wsBase}/chatrooms?chatRoomId=${chatRoomId}`;
}

function sortMessagesByDate(messages: MessageWithSenderName[]): MessageWithSenderName[] {
    return [...messages].sort(
        (a, b) => parseMessageDate(a.sentDate).getTime() - parseMessageDate(b.sentDate).getTime()
    );
}

interface MessageComposerProps {
    onSend: (content: string) => Promise<void>;
}

const MessageComposer: FC<MessageComposerProps> = ({onSend}) => {
    const [newMessage, setNewMessage] = useState("");
    const [sending, setSending] = useState(false);

    const handleSend = async () => {
        const trimmed = newMessage.trim();
        if (!trimmed || sending) return;

        setSending(true);
        try {
            await onSend(trimmed);
            setNewMessage("");
        } finally {
            setSending(false);
        }
    };

    const handleKeyDown = (event: React.KeyboardEvent<HTMLDivElement>) => {
        if (event.key === "Enter" && !event.shiftKey) {
            event.preventDefault();
            void handleSend();
        }
    };

    return (
        <div id="sendBox">
            <TextField
                fullWidth
                multiline
                maxRows={4}
                variant="outlined"
                label="Type a message"
                value={newMessage}
                onChange={(e) => setNewMessage(e.target.value)}
                onKeyDown={handleKeyDown}
                slotProps={{
                    htmlInput: {
                        "aria-label": "Type a message",
                        className: "custom-scrollbar",
                    },
                }}
            />
            <IconButton
                color="primary"
                onClick={() => void handleSend()}
                aria-label="Send message"
                disabled={sending}
            >
                <SendIcon/>
            </IconButton>
        </div>
    );
};

export const SelectedConversation: FC<SelectedConversationProps> = ({id, onConversationActivity}) => {
    const [chatroom, setChatroom] = useState<ChatRoom>();
    const [chatroomState, setChatroomState] = useState<"loading" | "ready" | "missing" | "error">("loading");
    const [currentConversation, setCurrentConversation] = useState<MessageWithSenderName[]>([]);
    const [chatroomName, setChatroomName] = useState<string>("");
    const [loading, setLoading] = useState<boolean>(false);
    const [pageNumber, setPageNumber] = useState<number>(1);
    const [pageSize] = useState<number>(10);
    const [hasMore, setHasMore] = useState<boolean>(true);
    const bottomRef = useRef<HTMLDivElement>(null);
    const conversationRef = useRef<HTMLUListElement>(null);
    const ws = useRef<WebSocket | null>(null);
    const userCache = useRef<Record<string, string>>({});
    const prependScrollHeightRef = useRef<number | null>(null);
    const shouldScrollToBottomRef = useRef(false);
    const hasMoreRef = useRef(hasMore);
    const loadingRef = useRef(loading);
    const pageNumberRef = useRef(pageNumber);
    const participantsRef = useRef<Account[]>([]);

    const [membersVersion, setMembersVersion] = useState<number>(0);
    const [participants, setParticipants] = useState<Account[]>([]);

    useEffect(() => {
        hasMoreRef.current = hasMore;
    }, [hasMore]);

    useEffect(() => {
        loadingRef.current = loading;
    }, [loading]);

    useEffect(() => {
        pageNumberRef.current = pageNumber;
    }, [pageNumber]);

    useEffect(() => {
        participantsRef.current = participants;
    }, [participants]);

    const addParticipant = useCallback((account: Account) => {
        setParticipants((prevParticipants) => {
            if (prevParticipants.some((participant) => participant.id === account.id)) {
                return prevParticipants;
            }
            return [...prevParticipants, account];
        });
    }, []);

    useLayoutEffect(() => {
        const conversationElement = conversationRef.current;
        if (!conversationElement) return;

        if (prependScrollHeightRef.current !== null) {
            conversationElement.scrollTop =
                conversationElement.scrollHeight - prependScrollHeightRef.current;
            prependScrollHeightRef.current = null;
            return;
        }

        if (shouldScrollToBottomRef.current) {
            bottomRef.current?.scrollIntoView({behavior: "auto"});
            shouldScrollToBottomRef.current = false;
        }
    }, [currentConversation]);

    useEffect(() => {
        if (!id) return;

        const fetchChatroom = async () => {
            setChatroomState("loading");

            try {
                const chatroom = await getChatroom(id);
                if (chatroom.membersIds) {
                    const localParticipants: Account[] = [];

                    for (const memberId of chatroom.membersIds) {
                        const member = await getAccount(memberId);
                        localParticipants.push(member);
                        addParticipant(member);
                        userCache.current[member.id] = member.name;
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
                setChatroomState("ready");
            } catch (error) {
                if ((error as {status?: number}).status === 404) {
                    setChatroomState("missing");
                    return;
                }

                console.error("Error fetching chatroom:", error);
                setChatroomState("error");
            }
        };

        fetchChatroom();
    }, [id, membersVersion, addParticipant]);

    const fetchOlderMessages = useCallback(async () => {
        if (!id || chatroomState !== "ready" || !hasMoreRef.current || loadingRef.current) {
            return;
        }

        setLoading(true);
        loadingRef.current = true;
        try {
            const currentPage = pageNumberRef.current;
            const conversation: Message[] = await getMessagesFromChatroom(id, currentPage, pageSize);

            if (conversation.length < pageSize) {
                setHasMore(false);
                hasMoreRef.current = false;
            }

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

            const orderedPage = sortMessagesByDate(conversationWithNames);
            setCurrentConversation((prev) => [...orderedPage, ...prev]);
            setPageNumber((prev) => {
                const next = prev + 1;
                pageNumberRef.current = next;
                return next;
            });
        } catch (error) {
            console.error("Error fetching older messages:", error);
        } finally {
            setLoading(false);
            loadingRef.current = false;
        }
    }, [id, chatroomState, pageSize]);

    useEffect(() => {
        if (!id || chatroomState !== "ready") {
            return;
        }

        shouldScrollToBottomRef.current = true;
        void fetchOlderMessages();

        const wsUrl = buildWebSocketUrl(id);
        ws.current = new WebSocket(wsUrl);

        ws.current.onmessage = async (event) => {
            try {
                const message = JSON.parse(event.data);
                if (!message.senderId) return;

                let account = participantsRef.current.find(
                    (participant) => participant.id === message.senderId
                );
                if (!account) {
                    account = await getAccount(message.senderId);
                    addParticipant(account);
                }

                const senderName = account.name;
                userCache.current[message.senderId] = senderName;
                const formattedSentDate = formatMessageDate(parseMessageDate(message.sentDate));

                const newMessageWithSenderName: MessageWithSenderName = {
                    ...message,
                    senderName,
                    formattedSentDate,
                };
                setCurrentConversation((prev) => {
                    if (prev.some((existing) => existing.id === newMessageWithSenderName.id)) {
                        return prev;
                    }
                    return [...prev, newMessageWithSenderName];
                });
                shouldScrollToBottomRef.current = true;
                if (message.sentDate) {
                    onConversationActivity?.(id, message.sentDate);
                } else {
                    onConversationActivity?.(id, new Date().toISOString());
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
    }, [id, chatroomState, fetchOlderMessages, addParticipant, onConversationActivity]);

    useEffect(() => {
        if (!id || chatroomState !== "ready") return;

        const conversationElement = conversationRef.current;
        if (!conversationElement) return;

        const handleScroll = () => {
            if (conversationElement.scrollTop === 0 && hasMoreRef.current && !loadingRef.current) {
                prependScrollHeightRef.current = conversationElement.scrollHeight;
                void fetchOlderMessages();
            }
        };

        const checkIfScrollable = () => {
            if (
                conversationElement.scrollHeight <= conversationElement.clientHeight &&
                hasMoreRef.current &&
                !loadingRef.current
            ) {
                shouldScrollToBottomRef.current = true;
                void fetchOlderMessages();
            }
        };

        conversationElement.addEventListener("scroll", handleScroll);
        checkIfScrollable();

        return () => {
            conversationElement.removeEventListener("scroll", handleScroll);
        };
    }, [id, chatroomState, fetchOlderMessages]);

    const handleSendMessage = useCallback(async (content: string) => {
        try {
            await sendMessage(id, content);
        } catch (error) {
            console.error("Error sending message:", error);
            throw error;
        }
    }, [id]);

    if (!id) {
        return (
            <div id="mainSelectedConversation">
                <div id="emptyConversation">Select a chat to start messaging</div>
            </div>
        );
    }

    if (chatroomState === "loading") {
        return (
            <div id="mainSelectedConversation">
                <AppLoader size={48}/>
            </div>
        );
    }

    if (chatroomState === "missing") {
        return (
            <MissingContent
                title="Chatroom not found"
                description="This chatroom may have been deleted or you no longer have access to it."
                backTo="/messages"
                backLabel="Back to messages"
            />
        );
    }

    if (chatroomState === "error") {
        return (
            <MissingContent
                title="Chatroom unavailable"
                description="We couldn't load this chatroom right now. Please try again later."
                backTo="/messages"
                backLabel="Back to messages"
            />
        );
    }

    return (
        <div id="mainSelectedConversation">
            <h1 id="selectedConversationTitle">
                {chatroomName}
                {chatroom?.type === ChatRoomType.General && (
                    <>
                        <span className="conversationMemberCount">
                            {participants.length} {participants.length === 1 ? 'member' : 'members'}
                        </span>
                        <GroupMembersPanel
                            chatroom={chatroom}
                            participants={participants}
                            onMembersChanged={() => setMembersVersion((version) => version + 1)}
                        />
                    </>
                )}
            </h1>
            <ul id="fullConversation" ref={conversationRef} className="custom-scrollbar">
                {currentConversation.map((message, index) => {
                    const previousMessage = currentConversation[index - 1];
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
                            key={message.id}
                            className={`messageRow${isFirstInGroup ? "" : " grouped"}`}
                        >
                            <div className="messageAvatarGutter">
                                {isFirstInGroup ? (
                                    <InteractiveUserAvatar
                                        userId={message.senderId}
                                        name={message.senderName}
                                        size={40}
                                        showName={false}
                                    />
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
            <MessageComposer onSend={handleSendMessage}/>
        </div>
    );
};

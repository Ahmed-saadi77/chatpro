import { useChatStore } from "../store/useChatStore";
import { useEffect, useRef } from "react";

import ChatHeader from "./ChatHeader";
import MessageInput from "./MessageInput";
import MessageSkeleton from "./skeletons/MessageSkeleton";
import { useAuthStore } from "../store/useAuthStore";
import { formatMessageTime } from "../lib/utils";

const ChatContainer = () => {
    const {
        messages,
        getMessages,
        isMessagesLoading,
        selectedUser,
        subscribeToMessages,
        unsubscribeFromMessages,
    } = useChatStore();
    const { authUser, connection } = useAuthStore();
    const messageEndRef = useRef(null);

    useEffect(() => {
        if (selectedUser?.id) {
            getMessages(selectedUser.id);
        }
    }, [selectedUser]);

    // Subscribe to SignalR messages only after connection is ready and selectedUser set
    useEffect(() => {
        if (!selectedUser?.id || !connection || connection.state !== 1) return;

        subscribeToMessages();

        return () => {
            unsubscribeFromMessages();
        };
    }, [selectedUser, connection]);

    // Scroll to bottom on new messages
    useEffect(() => {
        if (messageEndRef.current && messages.length > 0) {
            messageEndRef.current.scrollIntoView({ behavior: "smooth" });
        }
    }, [messages]);

    const getFullImageUrl = (url) => {
        if (!url) return "/avatar.png"; // fallback
        if (url.startsWith("http") || url.startsWith("data:")) return url;
        return `${import.meta.env.VITE_API_URL || "http://localhost:5158"}/${url}`;
    };

    if (isMessagesLoading) {
        return (
            <div className="flex-1 flex flex-col overflow-auto">
                <ChatHeader />
                <MessageSkeleton />
                <MessageInput />
            </div>
        );
    }

    return (
        <div className="flex-1 flex flex-col overflow-auto">
            <ChatHeader />

            <div className="flex-1 overflow-y-auto p-4 space-y-4">
                {messages.map((message, index) => (
                    <div
                        key={message.id}
                        className={`chat ${message.senderId === authUser.id ? "chat-end" : "chat-start"}`}
                        ref={index === messages.length - 1 ? messageEndRef : null} // only last message gets ref
                    >
                        <div className="chat-image avatar">
                            <div className="size-10 rounded-full border">
                                <img
                                    src={
                                        message.senderId === authUser.id
                                            ? getFullImageUrl(authUser.profilePictureUrl)
                                            : getFullImageUrl(selectedUser.profilePictureUrl)
                                    }
                                    alt="profile pic"
                                />
                            </div>
                        </div>

                        <div className="chat-header mb-1">
                            <time className="text-xs opacity-50 ml-1">
                                {formatMessageTime(message.createdAt)}
                            </time>
                        </div>

                        <div className="chat-bubble flex flex-col">
                            {message.imageUrl && (
                                <img
                                    src={getFullImageUrl(message.imageUrl)}
                                    alt="Attachment"
                                    className="sm:max-w-[200px] rounded-md mb-2"
                                />
                            )}
                            {message.text && <p>{message.text}</p>}
                        </div>
                    </div>
                ))}
            </div>

            <MessageInput />
        </div>
    );
};

export default ChatContainer;

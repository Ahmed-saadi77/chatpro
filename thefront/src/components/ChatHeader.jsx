import { X } from "lucide-react";
import { useAuthStore } from "../store/useAuthStore";
import { useChatStore } from "../store/useChatStore";

const ChatHeader = () => {
    const { selectedUser, setSelectedUser } = useChatStore();

    // Select only onlineUsers array from useAuthStore for performance
    const onlineUsers = useAuthStore((state) => state.onlineUsers);

    const getImageUrl = (path) => {
        return path?.startsWith("http")
            ? path
            : path
                ? `${import.meta.env.VITE_API_URL}${path}`
                : "/avatar.png";
    };

    if (!selectedUser) return null; // Defensive: no selected user

    return (
        <div className="p-2.5 border-b border-base-300">
            <div className="flex items-center justify-between">
                <div className="flex items-center gap-3">
                    {/* Avatar */}
                    <div className="avatar">
                        <div className="size-10 rounded-full relative">
                            <img
                                src={getImageUrl(selectedUser.profilePictureUrl)}
                                alt={selectedUser.fullName}
                                className="object-cover rounded-full w-full h-full"
                            />
                        </div>
                    </div>

                    {/* User info */}
                    <div>
                        <h3 className="font-medium">{selectedUser.fullName}</h3>
                        <p className="text-sm text-base-content/70">
                            {onlineUsers.includes(selectedUser.id?.toString()) ? "Online" : "Offline"}
                        </p>
                    </div>
                </div>

                {/* Close button */}
                <button onClick={() => setSelectedUser(null)}>
                    <X />
                </button>
            </div>
        </div>
    );
};

export default ChatHeader;

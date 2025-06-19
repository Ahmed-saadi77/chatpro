import { useEffect, useState } from "react";
import { useChatStore } from "../store/useChatStore";
import { useAuthStore } from "../store/useAuthStore";
import SidebarSkeleton from "./skeletons/SidebarSkeleton";
import { Users, X } from "lucide-react";

const Sidebar = () => {
    const {
        getUsers,
        users,
        selectedUser,
        setSelectedUser,
        isUsersLoading,
    } = useChatStore();

    const onlineUsers = useAuthStore((state) => state.onlineUsers);

    const [showOnlineOnly, setShowOnlineOnly] = useState(false);
    const [isOpen, setIsOpen] = useState(false);


    useEffect(() => {
        getUsers();
    }, []);

    useEffect(() => {
        const handleEsc = (e) => {
            if (e.key === "Escape") setIsOpen(false);
        };
        window.addEventListener("keydown", handleEsc);
        return () => window.removeEventListener("keydown", handleEsc);
    }, []);

    const getImageUrl = (path) => {
        return path?.startsWith("http")
            ? path
            : path
                ? `${import.meta.env.VITE_API_URL}${path}`
                : "/avatar.png";
    };

    const filteredUsers = showOnlineOnly
        ? users.filter((user) => onlineUsers.includes(user.id.toString()))
        : users;

    if (isUsersLoading) return <SidebarSkeleton />;

    return (
        <>
            {/* Hamburger Toggle Button */}
            <button
                onClick={() => setIsOpen(!isOpen)}
                className="lg:hidden p-2 fixed top-4 left-4 z-50 bg-base-100 text-base-content rounded-md shadow-md"
            >
                {isOpen ? <X className="w-6 h-6" /> : (
                    <svg
                        className="w-6 h-6"
                        fill="none"
                        stroke="currentColor"
                        strokeWidth="2"
                        viewBox="0 0 24 24"
                    >
                        <path strokeLinecap="round" strokeLinejoin="round" d="M4 6h16M4 12h16M4 18h16" />
                    </svg>
                )}
            </button>

            {/* Transparent Click Catcher for Small Screens */}
            {isOpen && (
                <div
                    onClick={() => setIsOpen(false)}
                    className="fixed inset-0 z-30 lg:hidden"
                />
            )}

            {/* Sidebar */}
            <aside
                className={`fixed lg:static top-0 left-0 h-full w-64 lg:w-72 bg-base-100 text-base-content border-r border-base-300 flex flex-col z-40 transform transition-transform duration-300 ${isOpen ? "translate-x-0" : "-translate-x-full"
                    } lg:translate-x-0`}
                onClick={(e) => e.stopPropagation()}
            >
                <div className="border-b border-base-300 w-full p-5">
                    <div className="flex items-center gap-2">
                        <Users className="size-6" />
                        <span className="font-medium hidden lg:block">Contacts</span>
                    </div>

                    <div className="mt-3 hidden lg:flex items-center gap-2">
                        <label className="cursor-pointer flex items-center gap-2">
                            <input
                                type="checkbox"
                                checked={showOnlineOnly}
                                onChange={(e) => setShowOnlineOnly(e.target.checked)}
                                className="checkbox checkbox-sm"
                            />
                            <span className="text-sm">Show online only</span>
                        </label>
                        <span className="text-xs text-zinc-500">
                            ({onlineUsers.length - 1} online)
                        </span>
                    </div>
                </div>

                <div className="overflow-y-auto w-full py-3 flex-1">
                    {filteredUsers.map((user) => (
                        <button
                            key={user.id}
                            onClick={() => {
                                setSelectedUser(user);
                                setIsOpen(false);
                            }}
                            className={`w-full p-3 flex items-center gap-3 hover:bg-base-300 transition-colors ${selectedUser?.id === user.id
                                ? "bg-base-300 ring-1 ring-base-300"
                                : ""
                                }`}
                        >
                            <div className="relative mx-auto lg:mx-0">
                                <img
                                    src={getImageUrl(user.profilePictureUrl)}
                                    alt={user.fullName}
                                    className="size-12 object-cover rounded-full"
                                />
                                {onlineUsers.includes(user.id.toString()) && (
                                    <span className="absolute bottom-0 right-0 size-3 bg-green-500 rounded-full ring-2 ring-zinc-900" />
                                )}
                            </div>
                            <div className="text-left min-w-0 flex-1 truncate">
                                <div className="font-medium truncate">{user.fullName}</div>
                                <div className="text-sm text-zinc-400 hidden lg:block">
                                    {onlineUsers.includes(user.id.toString())
                                        ? "Online"
                                        : "Offline"}
                                </div>
                            </div>
                        </button>
                    ))}
                </div>
            </aside>
        </>
    );
};

export default Sidebar;

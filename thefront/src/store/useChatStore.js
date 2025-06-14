import { create } from "zustand";
import toast from "react-hot-toast";
import { axiosInstance } from "../lib/axios";
import { useAuthStore } from "./useAuthStore";
import * as signalR from "@microsoft/signalr";

export const useChatStore = create((set, get) => ({
  messages: [],
  users: [],
  selectedUser: null,
  isUsersLoading: false,
  isMessagesLoading: false,
   onlineUsers: [],
  getUsers: async () => {
    set({ isUsersLoading: true });
    try {
      const res = await axiosInstance.get("/user/all");
      set({ users: res.data });
    } catch (error) {
      toast.error(error.response?.data?.message || "Failed to load users");
    } finally {
      set({ isUsersLoading: false });
    }
  },

  sendMessage: async (messageData) => {
    const { selectedUser, messages } = get();
    if (!selectedUser) return;

    try {
      const formData = new FormData();
      formData.append("receiverId", selectedUser.id);
      formData.append("text", messageData.text || "");
      if (messageData.imageFile) {
        formData.append("image", messageData.imageFile);
      }

      const res = await axiosInstance.post("/messages", formData, {
        headers: { "Content-Type": "multipart/form-data" },
      });

      set({ messages: [...messages, res.data] });
    } catch (error) {
      toast.error(error.response?.data?.message || "Failed to send message");
    }
  },

  getMessages: async (userId) => {
    set({ isMessagesLoading: true });
    try {
      const res = await axiosInstance.get(`/messages/${userId}`);
      set({ messages: res.data });
    } catch (error) {
      toast.error(error.response?.data?.message || "Failed to load messages");
    } finally {
      set({ isMessagesLoading: false });
    }
  },

  setSelectedUser: (selectedUser) => {
    const { unsubscribeFromMessages, subscribeToMessages } = get();

    unsubscribeFromMessages();
    set({ selectedUser, messages: [] });

    get().getMessages(selectedUser.id);
    // Wait a bit before subscribing to ensure connection is ready
    setTimeout(() => {
      subscribeToMessages();
    }, 300);
  },

  subscribeToMessages: () => {
    const connection = useAuthStore.getState().connection;
    if (!connection || connection.state !== signalR.HubConnectionState.Connected) {
      console.warn("SignalR connection not ready");
      return;
    }

    connection.off("ReceiveMessage");

    connection.on("ReceiveMessage", (newMessage) => {
      const { selectedUser, messages } = get();

      if (
        newMessage.senderId === selectedUser?.id ||
        newMessage.receiverId === selectedUser?.id
      ) {
        set({ messages: [...messages, newMessage] });
      }
    });
  },

  unsubscribeFromMessages: () => {
    const connection = useAuthStore.getState().connection;
    if (connection) {
      connection.off("ReceiveMessage");
    }
  },
}));

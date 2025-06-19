import axios from 'axios';
import { create } from 'zustand';
import { axiosInstance } from '../lib/axios';
import { toast } from 'react-hot-toast';
import * as signalR from '@microsoft/signalr';

const BASE_URL = import.meta.env.MODE === "development"
  ? "http://localhost:5000"
  : "http://localhost:5000";


export const useAuthStore = create((set, get) => ({
  authUser: null,
  isSigningUp: false,
  isLoggingIn: false,
  isUpdatingProfile: false,
  onlineUsers: [],
  isCheckingAuth: true,
  connection: null, // <-- renamed from socket to connection

  checkAuth: async () => {
    set({ isCheckingAuth: true });

    try {
      const res = await axiosInstance.get('/auth/check');
      set({ authUser: res.data });
      get().connectSignalR(); // connect after auth check
    } catch (error) {
      console.error('Error checking authentication:', error);
      set({ authUser: null });
    } finally {
      set({ isCheckingAuth: false });
    }
  },

  signup: async (data) => {
    set({ isSigningUp: true });
    try {
      const res = await axiosInstance.post("/auth/signup", data);
      set({ authUser: res.data });
      localStorage.setItem('token', res.data.token);
      toast.success("Account created successfully");
      get().connectSignalR(); // connect after signup
    } catch (error) {
      toast.error(error.response?.data?.message || "Signup failed");
    } finally {
      set({ isSigningUp: false });
    }
  },

  login: async (data) => {
    set({ isLoggingIn: true });
    try {
      const res = await axiosInstance.post("/auth/login", data);
      set({ authUser: res.data });
      localStorage.setItem('token', res.data.token);
      toast.success("Logged in successfully");
      get().connectSignalR(); // connect after login
    } catch (error) {
      toast.error("Login failed: " + (error.response?.data?.message || error.message));
    } finally {
      set({ isLoggingIn: false });
    }
  },

  logout: async () => {
    try {
      await axiosInstance.post("/auth/logout");
      localStorage.removeItem('token');
      set({ authUser: null });
      toast.success("Logged out successfully");
      get().disconnectSignalR();
    } catch (error) {
      toast.error(error.response?.data?.message || "Logout failed");
    }
  },

  updateProfile: async (file) => {
    set({ isUpdatingProfile: true });

    const formData = new FormData();
    formData.append("file", file);

    try {
      const res = await axiosInstance.post("/auth/update-profilepic", formData, {
        headers: {
          "Content-Type": "multipart/form-data",
        },
      });

      set({ authUser: res.data });
      toast.success("Profile updated successfully");
    } catch (error) {
      console.log("Error in update profile:", error);
      toast.error(error.response?.data?.message || "Something went wrong");
    } finally {
      set({ isUpdatingProfile: false });
    }
  },

  connectSignalR: () => {
  const { authUser, connection } = get();
  if (!authUser || connection) return; // prevent multiple connections

  const newConnection = new signalR.HubConnectionBuilder()
    .withUrl(`${BASE_URL}/chatHub?userId=${authUser.id}`, {
      accessTokenFactory: () => localStorage.getItem("token") || "",
    })
    .configureLogging(
      import.meta.env.MODE === "development"
        ? signalR.LogLevel.Information
        : signalR.LogLevel.None
    )
    .withAutomaticReconnect()
    .build();

  newConnection
    .start()
    .then(() => {
      set({ connection: newConnection });

      // Subscribe to online users update
      newConnection.on("GetOnlineUsers", (users) => {
        set({ onlineUsers: users });
      });

      // Add other event handlers if needed here
    })
    .catch((err) => console.error("SignalR Connection Error: ", err));
},


  disconnectSignalR: () => {
    const { connection } = get();
    if (connection) {
      connection.stop();
      set({ connection: null });
    }
  },
}));

export default useAuthStore;

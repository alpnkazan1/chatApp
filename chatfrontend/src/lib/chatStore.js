import { create } from "zustand";
import { toast } from "react-toastify";

const API_BASE_URL = import.meta.env.VITE_API_BASE_URL;

export const useChatStore = create((set) => ({
    selectedChat: null,
    messages: [],
    sharedPhotos: [],

    selectChat: async (chat) => {
        try {
            // Fetch messages and 5 photos from the server
            const [messagesRes, photosRes] = await Promise.all([
                fetch(`${API_BASE_URL}/api/messages/${chat.chatId}`),
                fetch(`${API_BASE_URL}/api/photos/${chat.chatId}?limit=5`) // Load first 10 thumbnails
            ]);

            const messagesData = await messagesRes.json();
            const photosData = await photosRes.json();
    
            // Store selected chat info and messages
            set({
                selectedChat: {
                    chatId: chat.chatId,
                    userName: chat.userName, // Taken from the chat list
                    avatar: chat.avatar // Taken from the chat list
                },
                messages: messagesData,
                sharedPhotos: photosData
            });
            
        } catch (error) {
            console.error("Failed to load messages", error);
            toast.error("Failed to load messages!");
        }
    },

    addMessage: (message) =>
        set((state) => ({ messages: [...state.messages, message] })),

    loadMorePhotos: async () => {
        const { selectedChat, sharedPhotos } = get();
        if (!selectedChat) return;

        try {
            const response = await fetch(
                `/api/chat/photos/${selectedChat.chatId}?limit=10&offset=${sharedPhotos.length}`
            );
            const morePhotos = await response.json();

            set((state) => ({
                sharedPhotos: [...state.sharedPhotos, ...morePhotos]
            }));
        } catch (error) {
            console.error("Failed to load more photos", error);
            toast.error("Failed to load more photos!");
        }
    },

    clearChat: () => set({ messages: [], sharedPhotos: [] })
}));

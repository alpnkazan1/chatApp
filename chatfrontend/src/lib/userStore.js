import { create } from "zustand";
const API_BASE_URL = import.meta.env.VITE_API_BASE_URL;

export const useUserStore = create((set) => ({
    currentUser: null,
    isLoading: true,
    accessToken: null,
    refreshToken: null,

    // Fetch user info from server
    fetchUserInfo: async () => {
        try {
            const response = await fetch(`${API_BASE_URL}/auth/check`, {
                method: "GET",
                credentials: "include", // Send cookies for authentication
            });

            if (response.ok) {
                const data = await response.json();

                // Ensure avatar is already a URL from the server
                set({
                    currentUser: {
                        id: data.user.id,         // Store user ID
                        userName: data.user.userName, // Store username
                        email: data.user.email,   // Store email
                        avatar: data.user.avatar, // Server should return a URL for avatar
                    },
                    isLoading: false,
                });
            } else {
                set({ currentUser: null, isLoading: false });
            }
        } catch (err) {
            console.log("Not authenticated error from userStore", err);
            set({ currentUser: null, isLoading: false });
        }
    },

    // Set Access Token on User
    setAccess: (token) => set({accessToken: token}),

    // Set Refresh Token on User
    setRefresh: (token) => set({refreshToken: token}),
    
    // Clear user data (log out)
    clearUser: () => set({ currentUser: null }),
}));

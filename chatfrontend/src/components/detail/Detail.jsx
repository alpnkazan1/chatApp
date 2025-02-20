import "./detail.css";
import { toast } from "react-toastify";
import { useState } from "react";
import { useUserStore } from "../../lib/userStore.js";
import { useChatStore } from "../../lib/chatStore.js";

const API_BASE_URL = import.meta.env.VITE_API_BASE_URL;

const Detail = () => {
    
    const { sharedPhotos, loadMorePhotos, selectedChat, clearChat } = useChatStore();
    const { clearUser } = useUserStore();

    const [showPhotos, setShowPhotos] = useState(false);
    const [privacyToggle, setPrivacyToggle] = useState(false);

    const togglePhotos = () => setShowPhotos(prev => !prev);
    const togglePrivacy = () => setPrivacyToggle(prev => !prev);

    const [isBlockModalOpen, setIsBlockModalOpen] = useState(false);
    const [deleteChat, setDeleteChat] = useState(false);

    const handleBlockUser = async () => {
        try {
            const response = await fetch(`${API_BASE_URL}/api/block-user`, {
                method: "POST",
                credentials: "include",
                headers: {
                    "Content-Type": "application/json"
                },
                body: JSON.stringify({ deleteChat })
            });

            if (!response.ok) throw new Error("Failed to block user");

            toast.success("User blocked successfully!");
            setIsBlockModalOpen(false);

            if (deleteChat) {
                useChatStore.getState().clearChat();
            }
        } catch (err) {
            console.error(err);
            toast.error("Failed to block user!");
        }
    };

    const handleDownload = (photoUrl, photoName) => {
        const link = document.createElement("a");
        link.href = photoUrl.replace("thumbnails", "fullsize");
        link.download = photoName;
        document.body.appendChild(link);
        link.click();
        document.body.removeChild(link);
    };

    const logout = async () => {
        try {
            await fetch(`${API_BASE_URL}/api/auth/logout`, {
                method: "POST",
                credentials: "include", // Ensures the cookie is included
            });
            clearUser(); // Clear user state using clearUser
            toast.success("Logged out!");
        } catch (err) {
            console.log(err);
            toast.error("Logout failed!");
        }
    };
    
    return(
        <div className="detail">
            <div className="user">
                <img 
                    src={selectedChat?.avatar || "./avatar.png"} 
                    alt="User Avatar"
                />
                <h2>{selectedChat?.userName || "User"}</h2>
            </div>
            <div className="info">
                <div className="option">
                    <div className="title">
                        <span>Chat Settings</span>
                        <img src="./arrowDown.png" alt=""/>
                    </div>
                </div>

                <div className="option">
                    <div className="title">
                        <span>Privacy & Help</span>
                        <img
                            src={privacyToggle ? "./arrowUp.png" : "./arrowDown.png"}
                            alt="privacy"
                            onClick={togglePrivacy}
                        />
                    </div>
                    {privacyToggle && (
                        <div className="privacy-text">
                            <p>
                                <br/>
                                This is a hobby project developed with the help and inspiration of React frontend code from  
                                <span>&#8194;</span>
                                <a href="https://github.com/safak" target="_blank" rel="noopener noreferrer">
                                    <strong>safak</strong>
                                </a>
                                <span>&#8194;</span> 
                                and ASP.NET backend code from
                                <span>&#8194;</span> 
                                <a href="https://github.com/teddysmithdev" target="_blank" rel="noopener noreferrer">
                                    <strong>teddysmithdev</strong>
                                </a>. <br/>
                                All the API endpoints, websocket communication, and database structure are completely my own design.<br/>
                                Please feel free to point out issues in my design and code.
                            </p>
                            <p>
                                For help or more information, visit the project's GitHub repository:
                                <a href="https://github.com/AlpereNkazan/chatApp" target="_blank" rel="noopener noreferrer">
                                    <strong>chatApp</strong>
                                </a>
                            </p>
                        </div>
                    )}
                </div>

                <div className="option">
                    <div className="title">
                        <span>Shared Photos</span>
                        <img onClick={togglePhotos} src={showPhotos ? "./arrowUp.png" : "./arrowDown.png"} alt="Toggle" />
                    </div>
                    {showPhotos && (
                        <div className="photos">
                            {sharedPhotos.map((photo, index) => (
                                <div className="photoItem" key={index}>
                                    <div className="photoDetail">
                                        <img src={photo.thumbnailUrl} alt="Shared" />
                                        <span>{photo.fileName || "Shared Image"}</span>
                                    </div>
                                    <img
                                        src="./download.png"
                                        alt="Download"
                                        className="icon"
                                        onClick={() => handleDownload(photo.fullImageUrl, photo.fileName)}
                                    />
                                </div>
                            ))}
                            {sharedPhotos.length > 0 && (
                                <button onClick={loadMorePhotos}>Load More</button>
                            )}
                        </div>
                    )}
                </div>

                <button onClick={() => setIsBlockModalOpen(true)}>Block User</button>
                {isBlockModalOpen && (
                    <div className="modal">
                        <p>Do you want to delete chat history as well?</p>
                        <label>
                            <input 
                                type="checkbox" 
                                checked={deleteChat} 
                                onChange={(e) => setDeleteChat(e.target.checked)} 
                            />
                            Delete chat history
                        </label>
                        <button onClick={handleBlockUser}>Confirm</button>
                        <button onClick={() => setIsBlockModalOpen(false)}>Cancel</button>
                    </div>
                )}
                <button className="logout" onClick={logout}>Log Out</button>
            </div>
        </div>
    )
}

export default Detail;
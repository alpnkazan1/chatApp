import "./chat.css";
import WebcamCapture from "./webcamCapture/WebcamCapture";
import MicCapture from "./micCapture/MicCapture";
import { useEffect, useState, useRef } from "react";
import * as signalR from "@microsoft/signalr"; // Import SignalR
import EmojiPicker from "emoji-picker-react";

import { useChatStore } from '../../lib/chatStore';
import { useUserStore } from '../../lib/userStore';

const API_BASE_URL = import.meta.env.VITE_API_BASE_URL;

const Chat = () => {
    const { currentUser, accessToken, } = useUserStore();
    const { selectedChat, addMessage, messages } = useChatStore();
    const [open, setOpen] = useState(false);
    const [text, setText] = useState("");
    const [isCameraOpen, setIsCameraOpen] = useState(false);
    const [selectedImage, setSelectedImage] = useState("");
    const [lastSeen, setLastSeen] = useState("");
    const [image, setImage] = useState(null);
    const endRef = useRef(null); //This is used to scroll to bottom of chat
    const fileInputRef = useRef(null);
    const [isMicOpen, setIsMicOpen] = useState(false);
    const [connection, setConnection] = useState(null); // SignalR Connection state

    useEffect(() => {
        // Early return on fail
        if (!selectedChat) return;

        endRef.current?.scrollIntoView({ behavior: "smooth" })

        // Start SignalR Connection
        const startSignalR = async () => {
            try {
                // Configure connection
                const hubConnection = new signalR.HubConnectionBuilder()
                    .withUrl(`${API_BASE_URL}/chatHub?chatId=${selectedChat.chatId}`, {
                        accessTokenFactory: () => accessToken,
                    })
                    .configureLogging(signalR.LogLevel.Information)
                    .build();
                
                //Set functions and log
                hubConnection.onclose(error => {
                  console.error("Websocket closed on backend: ", error)
                });
                // Listen for new incoming messages
                hubConnection.on("newMessage", (message) => {
                    addMessage(message);
                });

                // Listen for last seen updates
                hubConnection.on("lastSeenUpdate", (lastSeen) => {
                    setLastSeen(lastSeen);
                });
                
                // Start the connection
                await hubConnection.start();
                console.log("SignalR Connected.");
                setConnection(hubConnection);

            } catch (err) {
                console.error("SignalR connection error: ", err);
                toast.error("SignalR connection failed!");
            }
        };

        startSignalR();
      
        return () => {
            if (connection) {
                connection.stop();
            }
        };
    }, [selectedChat,accessToken, addMessage, setLastSeen]);

    const handleSendMessage = async () => {
        if (!text.trim() && !image) return;

        // Validate values before sending it to the server
        const receiverId = selectedChat.user1Id === currentUser.id ? selectedChat.user2Id : selectedChat.user1Id;

        const messageData = {
            chatId: selectedChat.chatId,
            receiverId: receiverId,
            messageText: text.trim(),
            fileFlag: image ? 1 : 0, // Set file flag if there's an image
            fileId: null,  // Implement file upload later
            fileExtension: image ? image.split('.').pop() : null //Gets the image ext
        };

        // Send Message
        try {
            await connection.invoke("SendMessage", messageData);
            setText("");
            setImage(null);
            setSelectedImage(null); // Reset image preview

        } catch (err) {
            console.error("Error sending message: ", err);
            // Handle errors - probably tell the user something went wrong
        }
    };

    const handleEmoji = e => {
        setText(prev => prev + e.emoji);
        setOpen(false);
    };

    const handleImageClick = () => {
        fileInputRef.current?.click();  // Open the file picker when the image icon is clicked
    };

    const handleImageChange = (event) => {
        const file = event.target.files[0];
        if (file) {
            setSelectedImage(URL.createObjectURL(file));  // Create a preview URL for the image
            setImage(file.name); //Store the name since fileID is not implement
        }
    };

    const handleCaptureImage = (imageSrc) => {
        // Handle the image received from webcam
        setSelectedImage(imageSrc);
        setImage(imageSrc) //Set the base64 url into image
    };

    return (
        <div className="chat">
            <div className="top">
                <div className="user">
                    <img src={selectedChat?.avatar || "./avatar.png"} alt="" />
                    <div className="texts">
                        <span>{selectedChat?.userName}</span>
                        <p>{lastSeen}</p>
                    </div>
                </div>
                <div className="icons">
                    <img src="./phone.png" alt="" />
                    <img src="./video.png" alt="" />
                    <img src="./info.png" alt="" />
                </div>
            </div>

            <div className="center">
                {messages.map((msg, index) => {
                    const isOwnMessage = msg.senderId === currentUser.id;
                    const isNotification = msg.senderId === 1; // Server messages

                    return isNotification ? (
                        <div key={index} className="message notification">
                            <p>{msg.text}</p>
                        </div>
                    ) : (
                        <div key={index} className={`message ${isOwnMessage ? "own" : ""}`}>
                            {msg.soundFileUrl ? (
                                <audio controls>
                                    <source src={msg.soundFileUrl} type="audio/mpeg" />
                                </audio>
                            ) : (
                                <>
                                    {msg.imageUrl && <img src={msg.imageUrl} alt="Sent Image" />}
                                    {msg.text && <p>{msg.text}</p>}
                                </>
                            )}
                            <span>{msg.timestamp}</span>
                        </div>
                    );
                })}
                <div ref={endRef}></div>
            </div>

            <div className="bottom">
                {isCameraOpen && (
                    <WebcamCapture
                        onCapture={handleCaptureImage}
                        onClose={() => setIsCameraOpen(false)}
                    />
                )}
                <div className="icons">
                    <img
                        src="./img.png"
                        alt="Pick Image"
                        onClick={handleImageClick}
                    />
                    <input
                        type="file"
                        ref={fileInputRef}
                        style={{ display: "none" }}
                        accept="image/*"
                        onChange={handleImageChange}
                    />
                    <img
                        src="./camera.png"
                        alt="Open Camera"
                        onClick={() => setIsCameraOpen(true)}
                    />
                    <img
                        src="./mic.png"
                        alt="Record Sound"
                        onClick={() => setIsMicOpen(true)}
                    />
                    {isMicOpen && (
                        <MicCapture
                            onClose={() => setIsMicOpen(false)}
                        />
                    )}
                </div>
                <div className="input-area">
                    {/* Image preview */}
                    {selectedImage && (
                        <div className="image-preview">
                            <img className="preview" src={selectedImage} alt="Selected" />
                            <button onClick={() => setSelectedImage(null)}>Remove Image</button>
                        </div>
                    )}
                    {/* Text input */}
                    <input
                        value={text}
                        onChange={(e) => setText(e.target.value)}
                        placeholder="Type a message..."
                        className="input-field"
                    />
                </div>
                <div className="emoji">
                    <img className="emoji-icon" src="./emoji.png" alt="" onClick={() => setOpen(prev => !prev)} />
                    <div className="picker">
                        <EmojiPicker open={open} onEmojiClick={handleEmoji} />
                    </div>
                </div>
                <button className="sendButton" onClick={handleSendMessage}>Send</button>
            </div>
        </div>
    )
}

export default Chat;
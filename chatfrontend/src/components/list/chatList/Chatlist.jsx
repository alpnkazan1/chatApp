import React, { useState, useEffect, useRef } from "react";
import "./chatList.css";
import AddUser from "./addUser/AddUser";
import { useUserStore } from "../../../lib/userStore";
import {useChatStore} from '../../../lib/chatStore';

function useOutsideClick(ref, callback) {
    useEffect(() => {
      function handleClickOutside(event) {
        if (ref.current && !ref.current.contains(event.target)) {
          callback(false); // Call the callback with `false` to close the component
        }
      }
  
      // Bind the event listener
      document.addEventListener("mousedown", handleClickOutside);
  
      return () => {
        // Unbind the event listener on cleanup
        document.removeEventListener("mousedown", handleClickOutside);
      };
    }, [ref, callback]);
};

const ChatList = () => {
    const [addMode,setAddMode] = useState(false);
    const addUserClickAwayRef = useRef(null);
    const {currentUser} = useUserStore();
    const { selectChat } = useChatStore();
    const [chats, setChats] = useState([]);

    useOutsideClick(addUserClickAwayRef, setAddMode);

    // Fetch chats and last messages from the DB
    useEffect(() => {
        // API call to get chats for the current user
        const fetchChats = async () => {
            const response = await fetch(`${API_BASE_URL}/api/chats/${currentUser.id}`); // Endpoint to get chat details
            const data = await response.json();
            // Sort chats by the timestamp of the last message in descending order
            const sortedChats = data.sort((a, b) => 
                b.lastMessageTime - a.lastMessageTime 
            );
            setChats(sortedChats);
        };

        fetchChats();
    }, [currentUser.id]);

    // Callback function to update chatlist when a new chat is added
    const handleNewChat = (newChat) => {
        setChats((prevChats) => [newChat, ...prevChats]); // Add new chat to the front
    };

    return(
        <div className="chatList">
            <div className="search">
                <div className="searchBar">
                    <img src="/search.png" alt="" />
                    <input type="text" placeholder="Search" />
                </div>
                <img 
                    src={addMode ? "./minus.png" : "./plus.png"} 
                    alt="" 
                    className="add"
                    onClick={()=>setAddMode(prev=>!prev)}
                />
            </div>
            {chats.map(chat => (
                <div 
                    key={chat.chatId} 
                    className={`item ${chat.blockFlag === 1 ? "blocked-by-you" : chat.blockFlag === 2 ? "blocked-by-other" : ""}`}
                    onClick={() => selectChat(chat)}
                >
                    {/**********************************************************************************
                        In case blocking occurs, no avatar will be given. Use default avatar then. 
                        If user blocked the other participant, their name should be in italic
                    ***********************************************************************************/}
                    <img src={chat.avatar || "./avatar.png"} alt="" className="avatar" />
                    <div className="chat-info">
                    <span className="chat-name">{chat.blockFlag === 1 ? <em>{chat.userName}</em> : chat.userName}</span>
                        <p className="chat-last-message">{chat.lastMessage}</p>
                    </div>
                    <span className="chat-time">{chat.lastMessageTime}</span>
                </div>
            ))}
            {addMode && <div ref={addUserClickAwayRef}><AddUser onChatCreated={handleNewChat}/></div>}
        </div>
    );
}

export default ChatList;
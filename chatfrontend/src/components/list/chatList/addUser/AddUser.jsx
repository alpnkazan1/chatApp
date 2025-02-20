import React, { useState } from "react";
import "./addUser.css";
import { useUserStore } from "../../../../lib/userStore";

const AddUser = (props) => {
  const {currentUser} = useUserStore();
  const [searchResult, setSearchResult] = useState(null);  // To hold search results
  const [searchError, setSearchError] = useState(null);    // To display any error messages

  const handleSearch = async (e) => {
      e.preventDefault();
      const formData = new FormData(e.target);  // Get data from the form
      const userName = formData.get("user");   // Extract the username input
      
      try {
          const response = await fetch(`${API_BASE_URL}/api/users/search?username=${userName}`); // Make API call to search user
          const data = await response.json();
          
          if (response.ok) {
              if (data) {
                  setSearchResult(data);  // If user found, set the result
              } else {
                  setSearchError("User not found");
                  setSearchResult(null); // If no user found, show error
              }
          } else {
              throw new Error(data.message || "Something went wrong");
          }
      } catch (err) {
          setSearchError(err.message);
          setSearchResult(null); // Reset search results in case of error
      }
  };

  const handleAddChat = async () => {
    try {
        // Assuming you have an API to add users to a new chat
        const response = await fetch(`${API_BASE_URL}/api/chats/create`, {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify({
                userId: searchResult.id, // Assuming searchResult has an id
                currentUserId: currentUser.id
            })
        });
        const data = await response.json();
        
        if (response.ok) {
          // Handle success (e.g., redirect to the new chat or show success message)
          console.log("Chat created successfully", data);
          props.onChatCreated(data.chat); // Pass the new chatId to the parent
        } else {
            throw new Error(data.message || "Failed to create chat");
        }
    } catch (err) {
        console.log(err);
    }
  };


  return (
    <div className="AddUser">
        <form onSubmit={handleSearch}>
            <input type="text" placeholder="Username" name="user" required />
            <button type="submit">Search</button>
        </form>

        {searchError && <div className="error">{searchError}</div>}

        {searchResult && (
            <div className="user">
                <div className="detail">
                    <img src={searchResult.avatar || "./avatar.png"} alt="" />
                    <span>{searchResult.userName}</span>
                </div>
                <button onClick={handleAddChat}>Add User</button>
            </div>
        )}
    </div>
  );
};

export default AddUser;

import Chat from "./components/chat/Chat";
import List from "./components/list/List";
import Detail from "./components/detail/Detail";
import Login from "./components/list/login/Login";
import Notification from "./components/list/notification/Notification";
import { useEffect } from "react";
import { useUserStore } from "./lib/userStore";

const App = () => {

  const { currentUser, fetchUserInfo, isLoading } = useUserStore(); // Zustand state

  useEffect(() => {
    fetchUserInfo();
  }, [fetchUserInfo]);
  
  if(isLoading) return <div className="loading">Loading...</div>

  return (
    <div className='container'>
    {currentUser ? (
      <>
      <List/>
      <Chat/>
      <Detail/>
      </>
    ) : (
      <Login/>
    )}
    <Notification/>
    </div>
  )
}

export default App;
import { ToastContainer } from "react-toastify";
import "react-toastify/ReactToastify.css"

const Notification = () =>{
    return (
        <div className="">
            <ToastContainer position="bottom-right" theme="dark"/>
        </div>
    )
}

export default Notification;
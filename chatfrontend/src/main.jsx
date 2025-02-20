import React from 'react'
import ReactDOM from 'react-dom/client'
import App from './App.jsx'
import './index.css'

if (import.meta.env.MODE === "development") {
  import("./mocks/browser")
    .then(({ worker }) => {
      // console.log("Worker imported", worker); // Debugging log
      return worker.start({
        onUnhandledRequest: 'bypass'
      });
    })
    .catch(console.error);
}


ReactDOM.createRoot(document.getElementById('root')).render(
  <React.StrictMode>
    <App />
  </React.StrictMode>,
)

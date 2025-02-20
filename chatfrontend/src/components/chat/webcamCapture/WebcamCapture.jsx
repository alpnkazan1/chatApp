import React, { useState, useRef } from "react";
import Webcam from "react-webcam";
import Draggable from 'react-draggable';
import "./webcamCapture.css";

const WebcamCapture = ({ onCapture, onClose }) => {
    const webcamRef = useRef(null);
  
    const captureImage = () => {
      if (!webcamRef.current) return;
  

      const imageSrc = webcamRef.current.getScreenshot(); // Get image as base64
      fetch(imageSrc)
        .then((res) => res.blob()) // Convert to blob
        .then((blob) => {
          const file = new File([blob], "captured-image.png", { type: "image/png" });
  
          // Pass captured image back to parent component
          onCapture(file);
          onClose(); // Close the modal after capturing the image
        });
    };
  
    return (
        <div className="webcam-container">
            <Draggable>
                <div className="webcam-overlay">
                <Webcam
                    audio={false}
                    ref={webcamRef}
                    screenshotFormat="image/png"
                    videoConstraints={{ facingMode: "user" }}
                    width="100%"
                    height="100%"
                />
                
                <button className="capture-btn" onClick={captureImage}>
                    Capture
                </button>
                <button className="close-btn" onClick={onClose}>
                    Close
                </button>
                </div>
            </Draggable>
        </div>
    );
};

export default WebcamCapture;
  
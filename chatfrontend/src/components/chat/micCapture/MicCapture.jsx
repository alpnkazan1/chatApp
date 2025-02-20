import React, { useState, useEffect, useRef } from "react";
import {useChatStore} from '../../../lib/chatStore';
import { ReactMic } from "react-mic";
import WaveSurfer from "wavesurfer.js";
import "./micCapture.css";

const MicCapture = ({ onClose }) => {
    const [isRecording, setIsRecording] = useState(false);
    const [audioBlob, setAudioBlob] = useState(null);
    const waveformRef = useRef(null);
    const wavesurferRef = useRef(null);
    const [isPlaying, setIsPlaying] = useState(false);
    const [soundFile, setSoundFile] = useState(null);
    const { selectedChat, addMessage, messages } = useChatStore();


    const handleSendSound = async () => {
        if (!soundFile) return;
    
        const message = {
            chatId: selectedChat.chatId,
            text: "",
            imageUrl: null,
            soundFileUrl: soundFile ? soundFile : null,
            timestamp: new Date().toISOString(),
            own: true,
        };
    
        socket.emit("userMessage", message);
        addMessage(message);
    
        setSoundFile(null);
    };

    const startRecording = () => {
        setAudioBlob(null);
        setIsRecording(true);
    };

    const stopRecording = () => {
        setIsRecording(false);
    };

    const onStop = (recordedBlob) => {
        setAudioBlob(recordedBlob.blob);
    };

    const handleSend = () => {
        if (audioBlob) {
            const audioFile = new File([audioBlob], "recording.mp3", { type: "audio/mpeg" });
            setSoundFile(audioFile);
            handleSendSound();
        }
    };

    useEffect(() => {
        if (audioBlob && waveformRef.current && !wavesurferRef.current) {
            wavesurferRef.current = WaveSurfer.create({
                container: waveformRef.current,
                waveColor: '#4F4A85',
                progressColor: '#383351',
                responsive: true,
                normalize: true,
                height: 100,
                barGap: 1
            });

            // Load the audioBlob into WaveSurfer
            const objectURL = URL.createObjectURL(audioBlob);
            wavesurferRef.current.load(objectURL);

            wavesurferRef.current.on('play', () => {
                setIsPlaying(true);
            });

            wavesurferRef.current.on('pause', () => {
                setIsPlaying(false);
            });
        }

        return () => {
            if (wavesurferRef.current) {
                wavesurferRef.current.destroy();
            }
        };
    }, [audioBlob]);  // Re-run when audioBlob changes

    const togglePlayPause = () => {
        if (wavesurferRef.current) {
            wavesurferRef.current.playPause();
        }
    };

    return (
        <div className="mic-capture">
            <ReactMic
                record={isRecording}
                onStop={onStop}
                mimeType="audio/mp3"
                className="mic-waveform"
                renderDrawing={false} // Disable ReactMic's preview waveform
            />
            <div className="controls">
                {!isRecording ? (
                    <button onClick={startRecording}>Start</button>
                ) : (
                    <button onClick={stopRecording}>Stop</button>
                )}
                {audioBlob && (
                    <div>
                        <div ref={waveformRef} className="waveform-container"></div>
                        <button onClick={togglePlayPause}>
                            {isPlaying ? "Pause" : "Play"}
                        </button>
                        <button onClick={handleSend}>Send</button>
                    </div>
                )}
                <button onClick={onClose}>Close</button>
            </div>
        </div>
    );
};

export default MicCapture;

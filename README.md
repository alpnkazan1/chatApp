# ChatApp v0.1

## Overview

ChatApp v0.1 is the initial public version of a real-time chat application. It is licensed under the MIT License, which can be reviewed in the `LICENSE` file. This project is currently under development and not fully functional yet. It is shared as proof of work and will be improved over time.

## Project Structure

ChatApp consists of three main components:

### React Frontend
- The frontend UI design is largely based on an open-source project by [safak](https://github.com/safak), but API calls and WebSocket functionalities have been implemented uniquely to fit the backend structure.
- The frontend code includes **MSW (Mock Service Worker) mocks** for standalone testing to some extent.

### ASP.NET 9.0 Backend
- The backend structure is inspired by a design from [teddysmithdev](https://github.com/teddysmithdev), but the API logic, WebSocket implementation, and overall architecture differ significantly.

### PostgreSQL Database
- The database structure is entirely custom-built and can be observed in `appSchematic.jpg`.

## Current Status

This version is not complete and does not yet function as intended. Several key features and implementations are missing or incomplete.

### Immediate Issues
- API call implementations on the backend are incomplete.
- Some API calls on the frontend need modification.
- WebSocket communication is not yet implemented or tested on the backend.
- Functionalities such as changing the profile picture and username are not implemented.
- The "Block User" functionality is missing in the backend and database structure, requiring significant modifications.

### Future Concerns
- The frontend CSS design needs improvement for better integration and a cohesive look.
- Real-time video and voice call functionalities are not yet implemented.

## License

This project is licensed under the **MIT License**. See the `LICENSE` file for details.

## Contributions & Feedback

As this project is still in its early stages, contributions and feedback are welcome. However, please note that many core functionalities are still under development.


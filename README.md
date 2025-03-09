# ChatApp v0.1

## Overview

ChatApp v0.1 is the initial public version of a real-time chat application. It is licensed under the MIT License, which can be reviewed in the `LICENSE` file. This project is currently under development and not fully functional yet. It is shared as proof of work and will be improved over time.

## Current Progress

* Current diagram is outdated! App structure is altered greatly since. I will update the diagram as soon as possible!

Main structure of both frontend and backend is completed. However, they are yet to be linked.
 - Frontend CSS still needs cleaning.
 - Some functionality is modified on backend as certain initial expectations were deemed illogical while backend was being implemented. They are noted and will be modified on frontend as well.
 - Some api calls are added to backend and should be integrated to frontend as well.
 - Manually tested some backend controllers on development. More comprehensive testing is needed.
 - Backend has seperation of concern (SoC) issues. Controllers include too much access. Created Repository classes to handle this issue. But, implementations are not done yet.
 - Modified the database structure greatly. A testing needs to be done to ensure no loose ends remain there.
 - There are many business logic considerations for the application that remains such as how user account deletion or chat deletion should be handled. Current code does not have a logically sound and realistic structure. However, it is proof of work. As such, these are secondary considerations.

## Project Structure

ChatApp consists of three main components:

### React Frontend
- The frontend UI design is largely based on an open-source project by [safak](https://github.com/safak), but API calls and WebSocket functionalities have been implemented uniquely to fit the backend structure.
- The frontend code includes **MSW (Mock Service Worker) mocks** for standalone testing to some extent.

### ASP.NET 9.0 Backend
- The backend structure is inspired by a design from [teddysmithdev](https://github.com/teddysmithdev), but the API logic, WebSocket implementation, and overall architecture differ significantly.

### PostgreSQL Database
- The database structure is entirely custom-built and can be observed in `appSchematic.jpg`. For implementation, you will likely need to change DefaultConnection.

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


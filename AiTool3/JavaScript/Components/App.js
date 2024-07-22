function App() {
    return (
        <>
            <style>
                {`
                    html, body, #root {
                        height: 100%;
                        margin: 0;
                        padding: 0;
                    }
                    .app-container {
                        display: flex;
                        flex-direction: column;
                        height: 100vh;
                    }
                    .main-content {
                        flex-grow: 1;
                        overflow-y: auto;
                        padding: 20px;
                        overflow-x: hidden;
                    }
                `}
            </style>
            <div className="app-container">
                <HeaderBar />
                <div className="main-content">
                    <MessagesPane />
                </div>
                <UserInputBar />
            </div>
        </>
    );
}

ReactDOM.render(
    React.createElement(App),
    document.getElementById('root')
);
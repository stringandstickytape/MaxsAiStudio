function App() {
    return ( 
        <>
            <style>
                {`
                    html, body, #root {
                        scroll-behavior: smooth;
                        height: 100%;
                        margin: 0;
                        padding: 0;
                        font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
                    }
                    .app-container {
                        scroll-behavior: smooth;
                        display: flex;
                        flex-direction: column;
                        height: 100vh;
                    }
                    .main-content {
                        flex-grow: 1;
                        overflow-y: scroll;
                        padding: 20px;
                        overflow-x: hidden;
                        scroll-behavior: smooth;
                            background-color: black;
                            color:white;
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
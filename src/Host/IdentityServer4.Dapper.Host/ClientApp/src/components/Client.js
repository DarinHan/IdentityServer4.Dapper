import React, { Component } from 'react';

export class Client extends Component {
    static displayName = Client.name;

    constructor(props) {
        super(props);
        this.state = { clients: [], loading: true };

        fetch('api/Clients', {
            method: 'post',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify({
                pageindex: 1,
                pagesize:10
            })
        })
            .then(response => response.json())
            .then(data => {
                this.setState({ clients: data.data, loading: false });
            });
    }

    static renderclientsTable(clients) {
        return (
            <table className='table table-striped'>
                <thead>
                    <tr>
                        <th>ClientId</th>
                        <th>ClientName</th>
                        <th>ClientUri</th>
                        <th>Enabled</th>
                        <th>LogoUri</th>
                    </tr>
                </thead>
                <tbody>
                    {clients.map(client =>
                        <tr key={client.clientId}>
                            <td>{client.clientId}</td>
                            <td>{client.clientName}</td>
                            <td>{client.clientUri}</td>
                            <td>{client.enabled}</td>
                            <td>{client.logoUri}</td>
                        </tr>
                    )}
                </tbody>
            </table>
        );
    }

    render() {
        let contents = this.state.loading
            ? <p><em>Loading...</em></p>
            : Client.renderclientsTable(this.state.clients);

        return (
            <div>
                <h1>clients</h1>
                {contents}
            </div>
        );
    }
}

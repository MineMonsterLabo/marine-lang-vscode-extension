import { workspace, ExtensionContext } from 'vscode';

import {
	LanguageClient,
	LanguageClientOptions,
	ServerOptions,
} from 'vscode-languageclient/node';

let client: LanguageClient;

export function activate(context: ExtensionContext) {
	let serverPath = context.asAbsolutePath("server/server/bin/Release/net6.0/server");

	let serverOptions: ServerOptions = {
		run: { command: serverPath },
		debug: {
			command: serverPath
		}
	};

	let clientOptions: LanguageClientOptions = {
		documentSelector: [{ scheme: 'file', language: 'marinescript' }],
		synchronize: {
			fileEvents: workspace.createFileSystemWatcher('{**/*.mrn,**/*.config.json}')
		}
	};

	client = new LanguageClient(
		'MarinelanguageServer',
		'Marine Language Server',
		serverOptions,
		clientOptions
	);

	client.start();
}

export function deactivate(): Thenable<void> | undefined {
	if (!client) {
		return undefined;
	}
	return client.stop();
}

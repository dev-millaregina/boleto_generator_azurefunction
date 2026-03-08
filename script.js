// Definir data mínima como hoje
const hoje = new Date().toISOString().split('T')[0];
document.getElementById('dataVencimento').setAttribute('min', hoje);

// Submeter formulário
document.getElementById('barcodeForm').addEventListener('submit', async (e) => {
    e.preventDefault();
    await gerarCodigoBarras();
});

async function gerarCodigoBarras() {
    const dataVencimento = document.getElementById('dataVencimento').value;
    const valor = parseFloat(document.getElementById('valor').value);

    if (!dataVencimento || !valor || valor <= 0) {
        mostrarErro('Por favor, preencha todos os campos corretamente.');
        return;
    }

    // Mostrar loading
    document.getElementById('loading').classList.add('active');
    document.getElementById('results').classList.remove('visible');
    ocultarMensagens();

    try {
        const response = await fetch('http://localhost:7071/api/barcode-generate', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
            },
            body: JSON.stringify({
                dataVencimento: dataVencimento,
                valor: valor
            })
        });

        if (!response.ok) {
            throw new Error(`Erro na requisição: ${response.status}`);
        }

        const data = await response.json();

        // Validar resposta
        if (!data.barcode || !data.imagemBase64) {
            throw new Error('Resposta inválida da API');
        }

        // Exibir resultados
        document.getElementById('barcodeValue').textContent = data.barcode;
        document.getElementById('barcodeImage').src = 'data:image/png;base64,' + data.imagemBase64;
        
        document.getElementById('loading').classList.remove('active');
        document.getElementById('results').classList.add('visible');
        document.getElementById('btnValidate').disabled = false;
        limparResultadoValidacao();
        
        mostrarSucesso('Código de barras gerado com sucesso!');

    } catch (error) {
        console.error('Erro:', error);
        document.getElementById('loading').classList.remove('active');
        mostrarErro('Erro ao gerar código de barras. Verifique se a API está rodando em http://localhost:7071');
    }
}

function copiarCodigo() {
    const codigo = document.getElementById('barcodeValue').textContent;
    navigator.clipboard.writeText(codigo).then(() => {
        const feedback = document.getElementById('copyFeedback');
        feedback.classList.add('show');
        setTimeout(() => {
            feedback.classList.remove('show');
        }, 2000);
    });
}

function baixarImagem() {
    const img = document.getElementById('barcodeImage');
    const link = document.createElement('a');
    link.href = img.src;
    link.download = `codigo_barras_${new Date().getTime()}.png`;
    link.click();
}

function limparFormulario() {
    document.getElementById('barcodeForm').reset();
    document.getElementById('results').classList.remove('visible');
    document.getElementById('btnValidate').disabled = true;
    limparResultadoValidacao();
    ocultarMensagens();
}

async function validarCodigoBarras() {
    const codigoBarras = document.getElementById('barcodeValue').textContent;
    
    if (!codigoBarras) {
        mostrarResultadoValidacao('Nenhum código de barras para validar', false);
        return;
    }

    mostrarResultadoValidacao('Validando código...', null);
    document.getElementById('btnValidate').disabled = true;

    try {
        const response = await fetch('http://localhost:7204/api/barcode-validade', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
            },
            body: JSON.stringify({
                barcode: codigoBarras
            })
        });

        const data = await response.json();
        const isValid = data.valido === true;
        
        let mensagem = isValid ? '✓ Código de barras é válido' : '✗ Código de barras é inválido';
        
        mostrarResultadoValidacao(mensagem, isValid);
    } catch (error) {
        console.error('Erro:', error);
        mostrarResultadoValidacao('Erro ao validar. Verifique se a API está rodando em http://localhost:7204', false);
    } finally {
        document.getElementById('btnValidate').disabled = false;
    }
}

function mostrarResultadoValidacao(mensagem, isValid) {
    const resultDiv = document.getElementById('validationResult');
    resultDiv.innerHTML = mensagem;
    
    if (isValid === null) {
        resultDiv.className = 'validation-result loading';
    } else if (isValid) {
        resultDiv.className = 'validation-result valid';
    } else {
        resultDiv.className = 'validation-result invalid';
    }
}

function limparResultadoValidacao() {
    const resultDiv = document.getElementById('validationResult');
    resultDiv.className = 'validation-result';
    resultDiv.textContent = '';
}

function mostrarErro(mensagem) {
    const errorDiv = document.getElementById('errorMessage');
    errorDiv.textContent = mensagem;
    errorDiv.classList.add('visible');
}

function mostrarSucesso(mensagem) {
    const successDiv = document.getElementById('successMessage');
    successDiv.textContent = mensagem;
    successDiv.classList.add('visible');
}

function ocultarMensagens() {
    document.getElementById('errorMessage').classList.remove('visible');
    document.getElementById('successMessage').classList.remove('visible');
}

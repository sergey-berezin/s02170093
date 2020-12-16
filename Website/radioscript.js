$(async () => {
    try {
        let response = await fetch('https://localhost:44353/rec')
        let json = await response.json()

        let radio = $('#radio')
        for (let i in json) {
            radio.append(
                $('<input>').prop({
                    type: 'radio',
                    name: 'label',
                    id: 'label',
                    value: json[i]
                })
            ).append(
                $('<label>').prop({
                    for: 'label'
                }).html('Label ' + (json[i].split(' '))[0] + ' ( ' + (json[i].split(' '))[1] + ' time(s) in database )')
            ).append(
                $('<br>')
            )
        }
    }

    catch (ex) {
        console.log(ex)
    }
})
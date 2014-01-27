/// <reference path="references.ts"/>


module Code {
    export function attachEntityLine(el: SF.EntityLine)
    {
        el.creating = () =>
        {
            $("#Author").SFControl<SF.EntityLine>().entityData(); 
            new SF.RuntimeInfo("#Author")

            return SF.FindNavigator.find("myQuery", el.options.prefix)
                .then(ed=> SF.ViewNavigator.view(el.options.prefix, ed));




                , 

            var fn = new FindNavigator({
                prefix: "fnPerson",
                webQueryName: "myQuery", 
                onOk: function (e) {
                    SF.Navgator.View("MyController/NewEntity", e, e =>
                        el.SetEntity(e))
                }
            }).openFinder();


            SF.Find("myQuery", e=>{



                SF.Navgator.View("MyController/NewEntity", e, e =>
                    el.SetEntity(e))); 

        };
    }
}